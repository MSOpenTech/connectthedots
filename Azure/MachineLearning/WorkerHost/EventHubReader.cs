//#define DEBUG_LOG

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
using Microsoft.WindowsAzure.Diagnostics;

namespace WorkerHost
{
    class EventHubReader
    {
        private const int DEFAULT_BUFFER_SIZE = 200;
        private const int MIN_COUNT_FOR_ANALYSIS = 10;

        private static int _bufferSize;

        private EventHubReceiver[] _receivers = null;
        private string _consumerGroupPrefix;
        //DateTime[] _receiversLastUpdate = null;
        private Task[] _tasks = null;
        private Dictionary<string, CircularBuffer<SensorDataContract>> _buffers;
        private object _lock = new object();
        //object _lockNoData = new object();
        private string _measureNameFilter;

        internal ManualResetEvent FailureEvent = new ManualResetEvent(false);

        public EventHubReader(int messagesBufferSize, string consumerGroupPrefix = "Local")
        {
            if (messagesBufferSize == 0)
            {
                _bufferSize = DEFAULT_BUFFER_SIZE;
            }
            else
            {
                _bufferSize = messagesBufferSize;
            }

            _consumerGroupPrefix = consumerGroupPrefix;
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

            string consumerGroupName = _consumerGroupPrefix + DateTime.UtcNow.Ticks.ToString();
            ConsumerGroupDescription consumerGroupDesc = nsmgr.CreateConsumerGroupIfNotExists(new ConsumerGroupDescription(hubName, consumerGroupName));

            EventHubClient client = EventHubClient.CreateFromConnectionString(connectionString, hubName);

            int numPartitions = desc.PartitionCount;
            _receivers = new EventHubReceiver[numPartitions];
            //_receiversLastUpdate = new DateTime[numPartitions];
            //for (int i = 0; i < numPartitions; i++)
            //{
            //    _receiversLastUpdate[i] = DateTime.UtcNow;
            //}

            _tasks = new Task[numPartitions];
            _buffers = new Dictionary<string, CircularBuffer<SensorDataContract>>();
            _measureNameFilter = measureNameFilter;

            for (int iPart = 0; iPart < desc.PartitionCount; iPart++)
            {
                EventHubReceiver receiver = client.GetConsumerGroup(consumerGroupName).CreateReceiver(
                    desc.PartitionIds[iPart], DateTime.UtcNow - TimeSpan.FromMinutes(2));
                _receivers[iPart] = receiver;

                Task<IEnumerable<EventData>> task = receiver.ReceiveAsync(1000, TimeSpan.FromSeconds(1));

                int thisPart = iPart;
                task.ContinueWith(new Action<Task<IEnumerable<EventData>>>((t) => OnTaskComplete(t, thisPart)));
                _tasks[iPart] = task;
            }
        }

        //void ProcessNoData()
        //{
        //    lock (_lockNoData)
        //    {
        //        DateTime now = DateTime.UtcNow;
        //        if (_receiversLastUpdate.All(d => now - d > TimeSpan.FromMinutes(3)))
        //        {
        //            Trace.TraceError("No data for the last 3 minutes. Reinitializing");
        //            FailureEvent.Set();
        //        }
        //    }
        //}

        void Process(int iPart, bool firstReport, IEnumerable<EventData> batch)
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
                        var payload = JsonConvert.DeserializeObject<IDictionary<string, object>>(line);

                        var sensorData = new SensorDataContract
                        {
                            DisplayName      = (string)  payload["displayname"],
                            Guid             = (string)  payload["guid"],
                            Location         = (string)  payload["location"],
                            MeasureName      = (string)  payload["measurename"],
                            Organization     = (string)  payload["organization"],
                            TimeCreated      = (DateTime)payload["timecreated"],
                            UnitOfMeasure    = (string)  payload["unitofmeasure"],
                            Value            = (double)  payload["value"]
                        };

                        var from = sensorData.UniqueId();

                        // Filter on MeasureName
                        if ((_measureNameFilter.Length == 0) ||
                             (_measureNameFilter.IndexOf(sensorData.MeasureName) >= 0))
                        {
                            lock (_lock)
                            {
                                CircularBuffer<SensorDataContract> buffer;
                                if (!_buffers.TryGetValue(from, out buffer))
                                {
                                    buffer = new CircularBuffer<SensorDataContract>(_bufferSize);
                                    _buffers.Add(from, buffer);
                                }

                                buffer.Add(sensorData);
#if DEBUG_LOG
                                Console.WriteLine("Data from device {0}, Total count: {1}", from, buffer.Count);
#endif
                            }
                        }
                    }
                    catch (Exception)
                    {
#if DEBUG_LOG
                        Trace.TraceError("Ignored invalid event data: {0}", line);
#endif
                    }
                }
            }

            //lock (_lockNoData)
            //{
            //    _receiversLastUpdate[iPart] = DateTime.UtcNow;
            //}
        }

        public Dictionary<string, SensorDataContract[]> GetHistoricData()
        {
            lock (_lock)
            {
                return _buffers.Where(kvp => kvp.Value.Count > MIN_COUNT_FOR_ANALYSIS)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetAll());
            }
        }

        void OnTaskComplete(Task<IEnumerable<EventData>> task, int iPart)
        {
            try
            {
                if (task.IsCompleted)
                {
                    IEnumerable<EventData> batch = task.Result;

                    if (batch != null && batch.Count() != 0)
                    {
#if DEBUG_LOG
                        Debug.WriteLine("Partition {0}, {1} events", iPart, batch.Count());
#endif
                        Process(iPart, false, batch);
                    }
                    else
                    {
                        //ProcessNoData();
                    }
                }
                else
                {
#if DEBUG_LOG
                    Trace.TraceError("Event hub reader {0} did not complete successfully : {1}", iPart,
                        task.Exception == null ? "" : task.Exception.ToString());
#endif
                    FailureEvent.Set();
                }

                Task<IEnumerable<EventData>> newTask = _receivers[iPart].ReceiveAsync(1000, TimeSpan.FromSeconds(1));
                int thisPart = iPart;
                newTask.ContinueWith(new Action<Task<IEnumerable<EventData>>>((t) => OnTaskComplete(t, thisPart)));
                this._tasks[iPart] = newTask;
            }
            catch (Exception e)
            {
#if DEBUG_LOG
                Trace.TraceError(e.ToString());
#endif
                FailureEvent.Set();
            }
        }
    }
}
