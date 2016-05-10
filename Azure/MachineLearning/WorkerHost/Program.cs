//#define DEBUG_LOG
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Newtonsoft.Json;
using WorkerHost.Data.Outputs;

namespace WorkerHost
{
    public class WorkerHost : RoleEntryPoint
    {
        public class Configuration
        {
            public string AlertEHConnectionString;
            public string AlertEHName;
            public string DeviceEHConnectionString;
            public string DeviceEHName;
            public string AnomalyDetectionApiUrl;
            public string AnomalyDetectionAuthKey;
            public string LiveId;
            public string MeasureNameFilter;

            public string TukeyThresh;
            public string ZscoreThresh;

            public bool UseMarketApi;
            public int MessagesBufferSize;
            public int AlertsIntervalSec;

            public string StorageConnectionString;
            public string BlobContainerName;
            public string BlobNamePrefix;
            public string SqlDatabaseConnectionString;
        }

        private static Analyzer        _analyzer;
        private static EventHubReader  _eventHubReader;
        private static Timer           _timer;

        private static BlobWriter _blobWriter;
        private static SQLOutputRepository _sqlOutputRepository;

        static void Main()
        {
            StartHost("LocalWorker");
        }

        public override void Run()
        {
            StartHost("WorkerRole");
        }

        private static void StartHost(string consumerGroupPrefix)
        {
            Trace.WriteLine("Starting Worker...");
#if DEBUG_LOG
            RoleEnvironment.TraceSource.TraceInformation("Starting Worker...");
#endif
            var config = new Configuration();

            config.AlertEHConnectionString = ConfigurationManager.AppSettings.Get("Microsoft.ServiceBus.ConnectionStringAlerts");
            config.AlertEHName = ConfigurationManager.AppSettings.Get("Microsoft.ServiceBus.EventHubAlerts");

            config.DeviceEHConnectionString = ConfigurationManager.AppSettings.Get("Microsoft.ServiceBus.ConnectionStringDevices");
            config.DeviceEHName = ConfigurationManager.AppSettings.Get("Microsoft.ServiceBus.EventHubDevices"); ;

            config.AnomalyDetectionApiUrl = ConfigurationManager.AppSettings.Get("AnomalyDetectionApiUrl");
            config.AnomalyDetectionAuthKey = ConfigurationManager.AppSettings.Get("AnomalyDetectionAuthKey");
            config.LiveId = ConfigurationManager.AppSettings.Get("LiveId");

            config.MeasureNameFilter = ConfigurationManager.AppSettings.Get("MeasureNameFilter");

            config.TukeyThresh = ConfigurationManager.AppSettings.Get("TukeyThresh");
            config.ZscoreThresh = ConfigurationManager.AppSettings.Get("ZscoreThresh");

            bool.TryParse(ConfigurationManager.AppSettings.Get("UseMarketApi"), out config.UseMarketApi);

            int.TryParse(ConfigurationManager.AppSettings.Get("MessagesBufferSize"), out config.MessagesBufferSize);
            int.TryParse(ConfigurationManager.AppSettings.Get("AlertsIntervalSec"), out config.AlertsIntervalSec);

            config.StorageConnectionString = ConfigurationManager.AppSettings.Get("Microsoft.Storage.ConnectionString");
            config.BlobContainerName = ConfigurationManager.AppSettings.Get("blobContainerName");
            config.BlobNamePrefix = ConfigurationManager.AppSettings.Get("blobNamePrefix");
            config.SqlDatabaseConnectionString = ConfigurationManager.AppSettings.Get("sqlDatabaseConnectionString");

            _analyzer = new Analyzer(config.AnomalyDetectionApiUrl, config.AnomalyDetectionAuthKey,
                config.LiveId, config.UseMarketApi, config.TukeyThresh, config.ZscoreThresh);

            _eventHubReader = new EventHubReader(config.MessagesBufferSize, consumerGroupPrefix);

            if (ConfigurationManager.AppSettings.Get("sendToBlob") == "true")
            {
                _blobWriter = new BlobWriter();
                if (_blobWriter.Connect(config.BlobNamePrefix, config.BlobContainerName, config.StorageConnectionString))
                {
                    _blobWriter = null;
                }
            }
            if (ConfigurationManager.AppSettings.Get("sendToSQL") == "true")
            {
                _sqlOutputRepository = new SQLOutputRepository(config.SqlDatabaseConnectionString);
            }
            Process(config);
        }

        public static void Process(Configuration config)
        {
            var alertEventHub = EventHubClient.CreateFromConnectionString(config.AlertEHConnectionString, config.AlertEHName);

            Trace.TraceInformation("Starting to receive messages...");
            _eventHubReader.Run(config.DeviceEHConnectionString, config.DeviceEHName, config.MeasureNameFilter);

            var timerInterval = TimeSpan.FromSeconds(1);
            var alertLastTimes = new Dictionary<string, DateTime>();

            TimerCallback timerCallback = state =>
            {

                var historicData = _eventHubReader.GetHistoricData();

                try
                {
                    var tasks = historicData.ToDictionary(kvp => kvp.Key, kvp => _analyzer.Analyze(kvp.Value));

                    Task.WaitAll(tasks.Values.ToArray());

                    List<SensorDataContract> alertsToSQl = new List<SensorDataContract>();

                    foreach (var kvp in tasks)
                    {
                        var key = kvp.Key;
                        var alerts = kvp.Value.Result;

                        DateTime alertLastTime;
                        if (!alertLastTimes.TryGetValue(@key, out alertLastTime))
                        {
                            alertLastTime = DateTime.MinValue;
                        }

                        
                        foreach (var alert in alerts)
                        {
                            if ((alert.Time - alertLastTime).TotalSeconds >= config.AlertsIntervalSec)
                            {
                                Trace.TraceInformation("Alert - {0}", alert.ToString());

                                string eventJSON = OutputResults(key, historicData[key].LastOrDefault(), alert);
                                alertEventHub.Send(
                                    new EventData(Encoding.UTF8.GetBytes(eventJSON)));

                                alertLastTime = alert.Time;
                                alertLastTimes[@key] = alertLastTime;

                                if (historicData[key].Length > 0)
                                {
                                    alertsToSQl.Add(historicData[key].Last());
                                    if (_blobWriter != null)
                                    {
                                        _blobWriter.WriteLine(eventJSON);
                                    }    
                                }
                            }
                        }
                    }

                    if (_sqlOutputRepository != null)
                    {
                        _sqlOutputRepository.ProcessEvents(alertsToSQl);
                    }
                    if (_blobWriter != null)
                    {
                        _blobWriter.Flush();
                    }
                }
                catch (Exception e)
                {
#if DEBUG_LOG
                    Trace.TraceError(e.Message);
                    Trace.TraceError(e.ToString());
#endif
                    //throw;
                }

                _timer.Change((int)timerInterval.TotalMilliseconds, Timeout.Infinite);
            };

            _timer = new Timer(timerCallback, null, Timeout.Infinite, Timeout.Infinite);
            _timer.Change(0, Timeout.Infinite);

            Trace.TraceInformation("Reading events from Event Hub (press ctrl+c to abort)");

            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            int index = WaitHandle.WaitAny(new[] {exitEvent, _eventHubReader.FailureEvent});

            Trace.TraceInformation("Exiting...");
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            Thread.Sleep(timerInterval);
            _timer.Dispose();
            _eventHubReader.Close();
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> collection, int batchSize)
        {
            List<T> nextbatch = new List<T>(batchSize);
            foreach (T item in collection)
            {
                nextbatch.Add(item);
                if (nextbatch.Count == batchSize)
                {
                    yield return nextbatch;
                    nextbatch = new List<T>();
                }
            }
            if (nextbatch.Count > 0)
                yield return nextbatch;
        }

        private static string OutputResults(string from, SensorDataContract sensorMeta, AnomalyRecord alert)
        {
            return JsonConvert.SerializeObject(
                new
                {
                    guid = sensorMeta.Guid,
                    displayname = sensorMeta.DisplayName,
                    measurename = sensorMeta.MeasureName,
                    unitofmeasure = sensorMeta.UnitOfMeasure,
                    location = sensorMeta.Location,
                    organization = sensorMeta.Organization,

                    timecreated = alert.Time.ToLocalTime(),
                    value = alert.Data,
                    alerttype = "MLModelAlert",
                    message = "Anomaly detected by Machine Learning model."
                });
        }

    }
}
