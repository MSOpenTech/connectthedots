#define DEBUG_LOG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace WorkerHost
{
    public class EventHubReader
    {
        private EventHubReceiver[] _receivers = null;
        private string _consumerGroupPrefix;

        private Task[] _tasks = null;
        private Action<string> _onMessage;

        internal ManualResetEvent FailureEvent = new ManualResetEvent(false);

        public EventHubReader(string consumerGroupPrefix, Action<string> onMessage)
        {
            _consumerGroupPrefix = consumerGroupPrefix;
            _onMessage = onMessage;
        }

        public void Close()
        {
            if (_receivers != null)
            {
                foreach (var r in _receivers)
                {
                    r.CloseAsync();
                }
            }
        }

        public void Run(string connectionString, string hubName, string measureNameFilter)
        {
            NamespaceManager nsmgr = NamespaceManager.CreateFromConnectionString(connectionString);
            EventHubDescription desc = nsmgr.GetEventHub(hubName);

            //we use already defined consumerGroup name to not reach limit on CG count
            string consumerGroupName = _consumerGroupPrefix;
            ConsumerGroupDescription consumerGroupDesc = nsmgr.CreateConsumerGroupIfNotExists(new ConsumerGroupDescription(hubName, consumerGroupName));

            EventHubClient client = EventHubClient.CreateFromConnectionString(connectionString, hubName);

            int numPartitions = desc.PartitionCount;
            _receivers = new EventHubReceiver[numPartitions];

            _tasks = new Task[numPartitions];

            for (int iPart = 0; iPart < desc.PartitionCount; iPart++)
            {
                EventHubReceiver receiver = client.GetConsumerGroup(consumerGroupName).CreateReceiver(
                    desc.PartitionIds[iPart], DateTime.UtcNow - TimeSpan.FromMinutes(2));
                _receivers[iPart] = receiver;

                int part = iPart;
                Task.Factory.StartNew((state) =>
                {
                    try
                    {
                        while (true)
                        {
                            var messages = _receivers[part].Receive(1000, TimeSpan.FromSeconds(1));
                            Process(messages);
                        }
                    }
                    catch (Exception ex)
                    {
                        //FailureEvent.Set();
                        Trace.TraceError("Ignored invalid event data: {0}");
                    }
                }, iPart);
            }
        }

        void Process(IEnumerable<EventData> batch)
        {
            UTF8Encoding enc = new UTF8Encoding();
            foreach (EventData e in batch)
            {
                string body = enc.GetString(e.GetBytes());
                string[] lines = body.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    try
                    {
                        _onMessage(line);
                    }
                    catch (Exception)
                    {
#if DEBUG_LOG
                        Trace.TraceError("Ignored invalid event data: {0}", line);
#endif
                    }
                }
            }
        }
    }
}
