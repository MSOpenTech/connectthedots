using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole
{

    class AlertsProcessor : IEventProcessor
    {
        private TopicClient devicesTopicClient;
        private static long beepDelay = 200000;
        private static long beepFreq = 1136;

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            return Task.FromResult<object>(null);
        }

        public Task OpenAsync(PartitionContext context)
        {
            var devicesTopicConnectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionStringDevicesService");
            var devicesTopicName = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.TopicDevices");

            var messagingFactory = MessagingFactory.CreateFromConnectionString(devicesTopicConnectionString);
            devicesTopicClient = messagingFactory.CreateTopicClient(devicesTopicName);

            return Task.FromResult<object>(null);
        }

        public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
           try {
               foreach (var message in messages) {
                   var eventBodyAsString = Encoding.UTF8.GetString(message.GetBytes());
                   Trace.WriteLine(eventBodyAsString);
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

                   foreach (var messagePlayload in messagePayloads)
                   {
                       switch (messagePlayload["alerttype"] as string)
                       {

                           case "TempSpike":
                               Trace.TraceInformation("Temperature alert!");
                               var command = new BrokeredMessage(String.Format("{{\"command\":\"beep\",\"freq\":{0},\"delay\":{1}}}", beepDelay, beepFreq));
                               command.Properties["deviceId"] = "all";

                               devicesTopicClient.Send(command);
                               break;
                       }
                   }
               }
           } catch (Exception e) {
               Trace.TraceError(e.Message);
           }
           return Task.FromResult<object>(null);
        }

        public static void ExceptionReceived(object sender, ExceptionReceivedEventArgs e)
        {
            Trace.TraceError("Exception received from EventHostProcessor: {0} - {1}, {2}", e.Exception, e.Action, sender);
        }

    }
}
