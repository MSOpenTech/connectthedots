//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Microsoft.ServiceBus.Messaging;

using Newtonsoft.Json;

namespace ConnectTheDotsWebSite
{
	class WebSocketEventProcessor : IEventProcessor
	{
		// Keep track of devices seen, and the last message received for each device
		public static ConcurrentDictionary<string, IDictionary<string, object>> g_devices =
			 new ConcurrentDictionary<string, IDictionary<string, object>>();

		// Keep a buffer of all messages for as long as the client UX needs them
		static TimeSpan bufferTimeInterval = new TimeSpan(0, 1, 0);

		// Message buffer (one per processor instance)
		static SortedList<DateTime, List<IDictionary<string, object>>> sortedDataBuffer = new SortedList<DateTime, List<IDictionary<string, object>>>();
		Stopwatch checkpointStopWatch;
		PartitionContext partitionContext;

//		public static bool GenerateAnomalies = false;

		struct MinMax { public double min; public double max;};
		private static Dictionary<string, MinMax> MinMaxValue = new Dictionary<string, MinMax>();


		public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> events)
		{
			try
			{
				var now = DateTime.UtcNow;

				foreach (var eventData in events)
				{
					// We don't care about messages that are older than bufferTimeInterval
					if ((eventData.EnqueuedTimeUtc + bufferTimeInterval) >= now)
					{
						// Get message from the eventData body and convert JSON string into message object
						string eventBodyAsString = Encoding.UTF8.GetString(eventData.GetBytes());

						// There can be several messages in one
						IList<IDictionary<string, object>> messagePayloads;
						try
						{
							// Attempt to deserialze event body as single JSON message
							messagePayloads = new List<IDictionary<string, object>> 
                            { 
                                JsonConvert.DeserializeObject<IDictionary<string, object>>(eventBodyAsString)
                            };
						}
						catch
						{
							// Not a single JSON message: attempt to deserialize as array of messages

							// Azure Stream Analytics Preview generates invalid JSON for some multi-values queries
							// Workaround: turn concatenated json objects (ivalid JSON) into array of json objects (valid JSON)
							if (eventBodyAsString.IndexOf("}{") >= 0)
							{
								eventBodyAsString = eventBodyAsString.Replace("}{", "},{");
							}
							if (!eventBodyAsString.EndsWith("]"))
								eventBodyAsString = eventBodyAsString + "]";
							if (!eventBodyAsString.StartsWith("["))
								eventBodyAsString = "[" + eventBodyAsString.Substring(eventBodyAsString.IndexOf("{"));

							messagePayloads = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(eventBodyAsString);
						}

						var rnd = new Random();
						foreach (var messagePayload in messagePayloads)
						{
							// Read time value
							if (messagePayload.ContainsKey("timecreated"))
							{
								messagePayload["time"] = messagePayload["timecreated"];
							}
							if (messagePayload.ContainsKey("timearrived"))
							{
								messagePayload["time"] = messagePayload["timearrived"];
							}
							// process an alert
                            if (messagePayload.ContainsKey("alerttype") && messagePayload.ContainsKey("timecreated"))
							{
                                Debug.Print("Alert message received!");

                                DateTime time = DateTime.Parse(messagePayload["timecreated"].ToString());
								// find the nearest point
								lock (sortedDataBuffer)
								{
                                    int idx = SearchHelper.FindFirstIndexGreaterThanOrEqualTo(sortedDataBuffer, time);
									bool found = false;
									string alertType = messagePayload["alerttype"] as string;

									if (idx >= sortedDataBuffer.Values.Count)
										idx = sortedDataBuffer.Values.Count - 1;

									while (idx >= 0)
									{
										List<IDictionary<string, object>> dictList = sortedDataBuffer.Values[idx];
										foreach (IDictionary<string, object> dict in dictList)
										{
											if ((dict.ContainsKey("guid") && messagePayload.ContainsKey("guid") && messagePayload["guid"].ToString() == dict["guid"].ToString()) ||
												(dict.ContainsKey("value") && dict.ContainsKey("displayname") && alertType.IndexOf(dict["displayname"] as string) >= 0))
											{
												// fill anomaly message
												messagePayload["value"] = dict["value"];
												messagePayload["guid"] = dict["guid"];
												messagePayload["displayname"] = dict["displayname"];
												if (!messagePayload.ContainsKey("time"))
												{
                                                    messagePayload["time"] = messagePayload["timecreated"];
												}
												found = true;
												break;
											}
										}
										if (found) break;
										idx--;
									}
								}
							}

							if (messagePayload.ContainsKey("guid"))
							{
								var guid = messagePayload["guid"].ToString();
								double val = Convert.ToDouble(messagePayload["value"]);

								if (!MinMaxValue.ContainsKey(guid))
									MinMaxValue.Add(guid, new MinMax { min = val, max = val });

								MinMax tmp = MinMaxValue[messagePayload["guid"].ToString()];
								if (tmp.min > val)
									tmp.min = val;
								if (tmp.max < val)
									tmp.max = val;
							}
							// We want to read the time value from the message itself.
							// If none is found we will use the enqueued time
							DateTime messageTimeStamp = new DateTime();
							if (messagePayload.ContainsKey("time"))
							{
								messageTimeStamp = DateTime.Parse(messagePayload["time"].ToString());

                                //if (GenerateAnomalies && rnd.Next(100) >= 95)
                                //{
                                //    messagePayload.Add("alerttype", "testType");
                                //    messagePayload.Add("dsplalert", "testAlert");
                                //    messagePayload.Add("message", "Anomaly detected by Azure ML model.");
                                //    messagePayload.Add("timestart", messagePayload["time"]);

                                //    // correct value
                                //    if (rnd.Next(2) == 1)
                                //        messagePayload["value"] = MinMaxValue[messagePayload["guid"].ToString()].max * (1.01 + 0.05 * rnd.Next(100) / 100);
                                //    else
                                //        messagePayload["value"] = MinMaxValue[messagePayload["guid"].ToString()].min * (0.99 - 0.05 * rnd.Next(100) / 100);
                                //}
							}
							else if (messagePayload.ContainsKey("timestart"))
								messageTimeStamp = DateTime.Parse(messagePayload["timestart"].ToString());
							else messageTimeStamp = eventData.EnqueuedTimeUtc;

							// Build up the list of devices seen so far (in lieu of a formal device repository)
							// Also keep the last message received per device (not currently used in the sample)
							if (messagePayload.ContainsKey("guid") && !messagePayload.ContainsKey("valueAvg"))
							{
								string guid = messagePayload["guid"].ToString();
								if (guid != null)
								{
									WebSocketEventProcessor.g_devices.TryAdd(guid, messagePayload);
								}
							}

							// Notify clients
							MyWebSocketHandler.SendToClients(messagePayload);

							// Buffer messages so we can resend them to clients that connect later
							// or when a client requests data for a different device

							// Lock to guard against concurrent reads from client resend
							lock (sortedDataBuffer)
							{
								if (!sortedDataBuffer.ContainsKey(messageTimeStamp))
									sortedDataBuffer.Add(messageTimeStamp, new List<IDictionary<string, object>>());

								sortedDataBuffer[messageTimeStamp].Add(messagePayload);
							}
						}
					}
					else
					{
						Debug.Print("Received old message timestamped:" + eventData.EnqueuedTimeUtc.ToString());
					}
				}

				//Call checkpoint every minute
				if (this.checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(1))
				{
					await context.CheckpointAsync();
					lock (this)
					{
						this.checkpointStopWatch.Restart();
					}

					// trim data buffer to keep only last 10 minutes of data
					lock (sortedDataBuffer)
					{
						DateTime oldDataPoint = now - bufferTimeInterval;
						// find the closest point
						int idx = SearchHelper.FindFirstIndexGreaterThanOrEqualTo(sortedDataBuffer, oldDataPoint);
						// trim
						while (idx > 0 && sortedDataBuffer.Count > 0 && sortedDataBuffer.Keys[0] <= oldDataPoint)
						{
							sortedDataBuffer.RemoveAt(0);
						}
					}
				}
			}
			catch (Exception e)
			{
				Trace.TraceError("Error processing events in EH {0}, partition {1}: {0}",
					 context.EventHubPath, context.Lease.PartitionId, e.Message);
			}
		}

		public Task OpenAsync(PartitionContext context)
		{
			Trace.TraceInformation(
				 String.Format("Opening processor for EH {0}, partition {1}.",
					  context.EventHubPath, context.Lease.PartitionId));
			lock (g_processors)
			{
				try
				{
					g_processors.Add(this);
				}
				catch (Exception e)
				{
					Trace.TraceError("Exception while adding processor for EH {0}, partition {1}: {2}",
						 context.EventHubPath, context.Lease.PartitionId, e.Message);
				}
			}

			this.partitionContext = context;
			this.checkpointStopWatch = new Stopwatch();
			this.checkpointStopWatch.Start();

			return Task.FromResult<object>(null);
		}

		public async Task CloseAsync(PartitionContext context, CloseReason reason)
		{
			Trace.TraceInformation(
				 String.Format("Closing processor for EH {0}, partition {1}. Reason: {2}",
					  context.EventHubPath, context.Lease.PartitionId, reason));

			lock (g_processors)
			{
				try
				{
					g_processors.Remove(this);
				}
				catch (Exception e)
				{
					Trace.TraceError(
						 String.Format("Exception while removing processor for EH {0}, partition {1}: {2}",
							  context.EventHubPath, context.Lease.PartitionId, e.Message));
				}
			}

			if (reason == CloseReason.Shutdown)
			{
				await context.CheckpointAsync();
			}
		}

		public static void ExceptionReceived(object sender, ExceptionReceivedEventArgs e)
		{
			Trace.TraceError("Exception received from EventHostProcessor: {0} - {1}, {2}", e.Exception, e.Action, sender);
		}

		static List<WebSocketEventProcessor> g_processors = new List<WebSocketEventProcessor>();

		// Retrieve buffered messages from all EH partitions (= processor instances)
		// Note: This needs to be partitioned and/or turned into a distributed call/cache 
		//  to support effective scale-out to multiple web client machines/VMs for large number of devices
		public static SortedList<DateTime, List<IDictionary<string, object>>> GetAllBufferedMessages()
		{
			//SortedList<DateTime, IDictionary<string, object>> allMessages = new SortedList<DateTime, IDictionary<string, object>>();
			//DateTime now = DateTime.UtcNow;

			//lock (g_processors)
			//{
			//    foreach (var processor in g_processors)
			//    {
			//        foreach(var item in processor.sortedDataBuffer)
			//        {
			//            if ((item.Key + bufferTimeInterval) >= now)
			//            {
			//                if (!allMessages.ContainsKey(item.Key))
			//                    allMessages.Add(item.Key, item.Value);
			//            }
			//        }
			//    }

			//}
			//return allMessages;
			return sortedDataBuffer;
		}
	}
	public static class SearchHelper
	{
		private static int BinarySearch<T>(IList<T> list, T value)
		{
			if (list == null)
				throw new ArgumentNullException("list");
			var comp = Comparer<T>.Default;
			int lo = 0, hi = list.Count - 1;
			while (lo < hi)
			{
				int m = (hi + lo) / 2;  // this might overflow; be careful.
				if (comp.Compare(list[m], value) < 0) lo = m + 1;
				else hi = m - 1;
			}
			if (comp.Compare(list[lo], value) < 0) lo++;
			return lo;
		}

		public static int FindFirstIndexGreaterThanOrEqualTo<T, U>
										  (this SortedList<T, U> sortedList, T key)
		{
			return BinarySearch(sortedList.Keys, key);
		}

	}
}
