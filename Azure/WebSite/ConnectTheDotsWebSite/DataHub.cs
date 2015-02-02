using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;

namespace ConnectTheDotsWebSite
{
    [HubName("DataHub")]
    public class DataHub : Hub
    {
        public static string DeviceFilter = null;
        public static List<string> DeviceFilterList = new List<string>();

        public override Task OnConnected()
        {
            Console.WriteLine("Client connected");
            return base.OnConnected();
        }

        public void Send(string message)
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
                            DeviceFilter = messageDictionary["DeviceName"] as string;

                            if (DeviceFilter == "clear")
                            {
                                DeviceFilterList.Clear();
                            }
                            else
                            {
                                DeviceFilterList.Add(DeviceFilter.ToLower());
                            }

                            break;
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

        public void SendFiltered(IDictionary<string, object> message)
        {
            if (!message.ContainsKey("dspl") ||
                    (DeviceFilterList != null && (
                        DeviceFilterList.Contains("all")
                        || DeviceFilterList.Contains(message["dspl"].ToString().ToLower())
                )))
            {
                this.Send(JsonConvert.SerializeObject(message));
            }
        }

        private void ResendDataToClient()
        {
            var bufferedMessages = EventProcessor.GetAllBufferedMessages();

            this.Send(JsonConvert.SerializeObject(new Dictionary<string, object> 
                { 
                    { "bulkData", true }
                }
            ));


            foreach (var message in bufferedMessages)
            {
                this.SendFiltered(message.Value);
            }

            this.Send(JsonConvert.SerializeObject(new Dictionary<string, object> 
                { 
                    { "bulkData", false}
                }
            ));
        }

        public static void SendToClients(IDictionary<string, object> message)
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<DataHub>();
            hubContext.Clients.All.onmessage(message);
        }
    }
}