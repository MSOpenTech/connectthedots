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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web;

using Microsoft.Web.WebSockets;

using Newtonsoft.Json;

namespace ConnectTheDotsWebSite
{
	public class WebSocketConnectController : ApiController
	{
		// GET api/<controller>
		public HttpResponseMessage Get(string clientId)
		{
			HttpContext.Current.AcceptWebSocketRequest(new MyWebSocketHandler());
			return Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
		}
	}

	sealed class MyWebSocketHandler : WebSocketHandler
	{
		private static readonly WebSocketCollection _clients = new WebSocketCollection();

		public List<string> DeviceFilterList = new List<string>();

		public MyWebSocketHandler()
		{
		}

		public override void OnOpen()
		{
			lock (_clients)
			{
				_clients.Add(this);
			}
			ResendDataToClient();
		}

		public override void OnClose()
		{
			lock (_clients)
			{
				_clients.Remove(this);
			}
			base.OnClose();
		}

		public override void OnMessage(string message)
		{
			try
			{
				var messageDictionary = (IDictionary<string, object>)
					 JsonConvert.DeserializeObject(message, typeof(IDictionary<string, object>));

				if (messageDictionary.ContainsKey("MessageType"))
				{
					switch (messageDictionary["MessageType"] as string)
					{
						case "LiveDataSelection":
							string deviceFilter = messageDictionary["DeviceGUIDs"] as string;

							if (deviceFilter == "clear")
							{
								DeviceFilterList.Clear();
							}
							else
							{
								string[] guids = deviceFilter != null ? deviceFilter.Split(',') : null;
								if (guids == null) { DeviceFilterList.Add("All"); }
								else
								{
									foreach (var guid in guids)
									{
										DeviceFilterList.Add(guid.ToLower());
									}
								}
							}

							break;
                        //case "AnomaliesControl":
                        //    string newState = messageDictionary["State"] as string;

                        //    if (newState == "generate")
                        //    {
                        //        WebSocketEventProcessor.GenerateAnomalies = true;
                        //    }
                        //    else
                        //    {
                        //        WebSocketEventProcessor.GenerateAnomalies = false;
                        //    }

                        //    return;
						default:
							Trace.TraceError("Client message with unknown message type: {0} - {1}", messageDictionary["MessageType"], message);
							break;
					}
				}
				else
				{
					Trace.TraceError("Client message without message type: {0}", message);
				}
			}
			catch (Exception e)
			{
				Trace.TraceError("Error processing client message: {0} - {1}", e.Message, message);
			}
			ResendDataToClient();
		}

		private void ResendDataToClient()
		{
			// exit bulk mode
			this.Send(JsonConvert.SerializeObject(new Dictionary<string, object> 
                    { 
                        { "bulkData", false }
                    }
			));

			return;

			var bufferedMessages = WebSocketEventProcessor.GetAllBufferedMessages();

			// collect all guids for bulk data

			this.Send(JsonConvert.SerializeObject(new Dictionary<string, object> 
                { 
                    { "bulkData", true }
                }
			));

			lock (bufferedMessages)
			{
				try
				{
					//                IList<string> filteredMessages = new List<string>();
					foreach (var messageList in bufferedMessages.Values)
					{
						foreach (var message in messageList)
						{
							if (Filter(message))
							{
								//                       filteredMessages.Add(JsonConvert.SerializeObject(message));
								this.Send(JsonConvert.SerializeObject(message));
							}
						}
					}
					//foreach (var payload in filteredMessages)
					//{
					//    this.Send(payload);
					//}
				}
				finally
				{
					this.Send(JsonConvert.SerializeObject(new Dictionary<string, object> 
                    { 
                        { "bulkData", false }
                    }
					));
				}
			}
		}

		private bool Filter(IDictionary<string, object> message)
		{
			DateTime messageTime = new DateTime();
			TimeSpan bufferTime = new TimeSpan(0, 10, 0);
			DateTime now = DateTime.UtcNow;

			if (message.ContainsKey("time"))
				messageTime = DateTime.Parse(message["time"].ToString());
			else if (message.ContainsKey("timestart"))
				messageTime = DateTime.Parse(message["timestart"].ToString());

			if (
					  !message.ContainsKey("guid") ||
					  (
							(
							 this.DeviceFilterList.Contains("all") ||
							 this.DeviceFilterList.Contains(message["guid"].ToString().ToLower())
							)
					  )
				 )
			{
				if (messageTime + bufferTime < now)
					return false;
				return true;
			}

			return false;
		}

		public static void SendToClients(IDictionary<string, object> message)
		{
			// snapshot the current clients
			WebSocketHandler[] clients;
			lock (_clients)
			{
				clients = _clients.ToArray<WebSocketHandler>();
			}

			//send
			foreach (MyWebSocketHandler client in clients)
			{
				if (client.Filter(message))
				{
					client.Send(JsonConvert.SerializeObject(message));
				}
			}
		}


	}

}