using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.ServiceBus.Messaging;

namespace WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole is running");
            
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole has been started");

            var alertsEventHubName = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.EventHubAlerts");
            var alertsEventHubConnectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionStringAlerts");
            var alertsStorageConnectionString = CloudConfigurationManager.GetSetting("Microsoft.Storage.ConnectionString");

            var alertsEventHubClient = EventHubClient.CreateFromConnectionString(alertsEventHubConnectionString, alertsEventHubName);

            var processorHostDevices = new EventProcessorHost("SensorsWorkerRole", alertsEventHubName, "$default",
                alertsEventHubConnectionString, alertsStorageConnectionString);

            var options = new EventProcessorOptions();
                options.InitialOffsetProvider = (partitionId) => DateTime.UtcNow;
                options.ExceptionReceived += AlertsProcessor.ExceptionReceived;

            processorHostDevices.RegisterEventProcessorAsync<AlertsProcessor>(options).Wait();

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole has stopped");
        }

    }
}
