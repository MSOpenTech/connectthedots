//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, Inc.  All rights reserved.
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
