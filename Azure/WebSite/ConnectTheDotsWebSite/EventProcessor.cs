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
    class EventProcessor : IEventProcessor
    {
        // Keep track of devices seen, and the last message received for each device
        public static ConcurrentDictionary<string, IDictionary<string, object>> g_devices =
            new ConcurrentDictionary<string, IDictionary<string, object>>();

        // Keep a buffer of all messages for as long as the client UX needs them
        TimeSpan bufferTimeInterval = new TimeSpan(0, 10, 0);

        // Message buffer (one per processor instance)
        static SortedList<DateTime, IDictionary<string, object>> sortedDataBuffer = new SortedList<DateTime, IDictionary<string, object>>();
        Stopwatch checkpointStopWatch;
        PartitionContext partitionContext;

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
                                eventBodyAsString = "[" + eventBodyAsString;

                            messagePayloads = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(eventBodyAsString);
                        }

                        foreach (var messagePayload in messagePayloads)
                        {
                            // We want to read the time value from the message itself.
                            // If none is found we will use the enqueued time
                            DateTime messageTimeStamp = new DateTime();
                            if (messagePayload.ContainsKey("time"))
                                messageTimeStamp = DateTime.Parse(messagePayload["time"].ToString());
                            else if (messagePayload.ContainsKey("timeStart"))
                                messageTimeStamp = DateTime.Parse(messagePayload["timeStart"].ToString());
                            else messageTimeStamp = eventData.EnqueuedTimeUtc;

                            // Build up the list of devices seen so far (in lieu of a formal device repository)
                            // Also keep the last message received per device (not currently used in the sample)
                            string deviceName = null;
                            if (messagePayload.ContainsKey("dspl"))
                            {
                                deviceName = messagePayload["dspl"] as string;
                                if (deviceName != null)
                                {
                                    EventProcessor.g_devices.TryAdd(deviceName, messagePayload);
                                }
                            }

                            // Notify clients
                            DataHub.SendToClients(messagePayload);

                            // Buffer messages so we can resend them to clients that connect later
                            // or when a client requests data for a different device

                            // Lock to guard against concurrent reads from client resend
                            lock (sortedDataBuffer)
                            {
                                if (!sortedDataBuffer.ContainsKey(messageTimeStamp))
                                    sortedDataBuffer.Add(messageTimeStamp, messagePayload);
                            }
                        }
                    }
                }

                //Call checkpoint every minute
                if (this.checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(1))
                {
                    await context.CheckpointAsync();
                    lock (this)
                    {
                        this.checkpointStopWatch.Reset();
                    }

                    // trim data buffer to keep only last 10 minutes of data
                    lock (sortedDataBuffer)
                    {
                        SortedList<DateTime, IDictionary<string, object>> tempBuffer = new SortedList<DateTime, IDictionary<string, object>>();
                        foreach (var item in sortedDataBuffer)
                        {
                            if (item.Key + bufferTimeInterval >= now)
                            {
                                tempBuffer.Add(item.Key, item.Value);
                            }
                        }

                        sortedDataBuffer = tempBuffer;
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

        static List<EventProcessor> g_processors = new List<EventProcessor>();

        // Retrieve buffered messages from all EH partitions (= processor instances)
        // Note: This needs to be partitioned and/or turned into a distributed call/cache 
        //  to support effective scale-out to multiple web client machines/VMs for large number of devices
        public static SortedList<DateTime, IDictionary<string, object>> GetAllBufferedMessages()
        {
            return sortedDataBuffer;
        }
    }
}
