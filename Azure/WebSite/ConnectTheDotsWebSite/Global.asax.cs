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
using System.Diagnostics;
using System.Web.Http;
using System.Web.Routing;

using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;

namespace ConnectTheDotsWebSite
{
    public class Global : System.Web.HttpApplication
    {
        EventProcessorHost processorHostDevices;
        EventProcessorHost processorHostAlerts;

        protected void Application_Start(Object sender, EventArgs e)
        {

            // Set up a route for WebSocket requests
            RouteTable.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            var serviceBusConnectionStringDevices = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionStringDevices");
            var serviceBusConnectionStringAlerts = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionStringAlerts");
            if (String.IsNullOrEmpty(serviceBusConnectionStringAlerts))
            {
                serviceBusConnectionStringAlerts = serviceBusConnectionStringDevices;
            }

            var eventHubDevices = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.EventHubDevices");
            var eventHubDevicesConsumerGroup = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.EventHubDevicesConsumerGroup");

            var eventHubAlerts = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.EventHubAlerts");
            var eventHubAlertsConsumerGroup = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.EventHubAlertsConsumerGroup");

            var storageConnectionString = CloudConfigurationManager.GetSetting("Microsoft.Storage.ConnectionString");

            if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
            {
                // Assume we are running local: use different consumer groups to avoid colliding with a cloud instance
                eventHubDevicesConsumerGroup += "local";
                eventHubAlertsConsumerGroup += "local";
            }


            Trace.TraceInformation("Creating EventProcessorHost: {0}, {1}, {2}", this.Server.MachineName, eventHubDevices, eventHubDevicesConsumerGroup);
            processorHostDevices = new EventProcessorHost(this.Server.MachineName,
                eventHubDevices.ToLowerInvariant(),
                eventHubDevicesConsumerGroup.ToLowerInvariant(),
                serviceBusConnectionStringDevices,
                storageConnectionString
                );

            var options = new EventProcessorOptions();
            options.ExceptionReceived += EventProcessor.ExceptionReceived;
            options.InitialOffsetProvider = (partitionId) => DateTime.UtcNow;

            Trace.TraceInformation("Registering EventProcessor for Devices");
            processorHostDevices.RegisterEventProcessorAsync<EventProcessor>(options).Wait();

            Trace.TraceInformation("Creating EventProcessorHost: {0}, {1}, {2}", this.Server.MachineName, eventHubAlerts, eventHubAlertsConsumerGroup);
            processorHostAlerts = new EventProcessorHost(this.Server.MachineName,
                eventHubAlerts.ToLowerInvariant(),
                eventHubAlertsConsumerGroup.ToLowerInvariant(),
                serviceBusConnectionStringAlerts,
                storageConnectionString
            );
            Trace.TraceInformation("Registering EventProcessor for Alerts");
            processorHostAlerts.RegisterEventProcessorAsync<EventProcessor>(options).Wait();
        }

        protected void Application_End(Object sender, EventArgs e)
        {
            Trace.TraceInformation("Unregistering EventProcessorHosts");
            processorHostDevices.UnregisterEventProcessorAsync().Wait();
            processorHostAlerts.UnregisterEventProcessorAsync().Wait();
        }

    }
}