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
using Microsoft.ServiceBus;

namespace ConnectTheDotsWebSite
{
    public struct EventHubSettings
    {
        public string name { get; set; }
        public string connectionString { get; set; }
        public string consumerGroup { get; set; }
        public EventProcessorHost processorHost { get; set; }
        public EventProcessorOptions processorHostOptions { get; set; }
        public EventHubClient client { get; set; }
        public NamespaceManager namespaceManager { get; set; }
        public string storageConnectionString { get; set; }
    }

    public class Global : System.Web.HttpApplication
    {
        EventHubSettings eventHubDevicesSettings;
        EventHubSettings eventHubAlertsSettings;

        protected void Application_Start(Object sender, EventArgs e)
        {

            // Set up a route for WebSocket requests
            RouteTable.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Read connectiong strings and Event Hubs names from app.config file
            GetAppSettings();

            // Create EventProcessorHost clients
            CreateEventProcessorHostClient(ref eventHubDevicesSettings);
            CreateEventProcessorHostClient(ref eventHubAlertsSettings);
        }

        protected void Application_End(Object sender, EventArgs e)
        {
            Trace.TraceInformation("Unregistering EventProcessorHosts");
            eventHubDevicesSettings.processorHost.UnregisterEventProcessorAsync().Wait();
            eventHubAlertsSettings.processorHost.UnregisterEventProcessorAsync().Wait();
        }
        private void CreateEventProcessorHostClient(ref EventHubSettings eventHubSettings)
        {
            Trace.TraceInformation("Creating EventProcessorHost: {0}, {1}, {2}", this.Server.MachineName, eventHubSettings.name, eventHubSettings.consumerGroup);
            eventHubSettings.client = EventHubClient.CreateFromConnectionString(eventHubSettings.connectionString,
                                                                                eventHubSettings.name);

            // Delete and recreate the consumer group
            // this allows to ensure we will start receiving only fresh messages when the site starts
            try
            {
                foreach (ConsumerGroupDescription consumerGroupDesc in eventHubSettings.namespaceManager.GetConsumerGroups(eventHubSettings.client.Path))
                {
                    // We remove any previously created consumergroups containing the word WebSite in the name
                    if (consumerGroupDesc.Name.Contains("WebSite") && !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"))
                        || consumerGroupDesc.Name.Contains("local") && String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
                        eventHubSettings.namespaceManager.DeleteConsumerGroup(eventHubSettings.name, consumerGroupDesc.Name);
                }
            }
            catch
            {
                // Error happened while trying to delete old ConsumerGroups.
                Debug.Print("Error happened while trying to delete old ConsumerGroups");
            }
            finally
            {
                // We create a new consumer group with a new mame each time to 
                eventHubSettings.consumerGroup += DateTime.UtcNow.Ticks.ToString();
                eventHubSettings.namespaceManager.CreateConsumerGroupIfNotExists(eventHubSettings.name, eventHubSettings.consumerGroup);
            }

            eventHubSettings.processorHost = new EventProcessorHost(this.Server.MachineName,
                                                          eventHubSettings.client.Path,
                                                          eventHubSettings.consumerGroup.ToLowerInvariant(),
                                                          eventHubSettings.connectionString,
                                                          eventHubSettings.storageConnectionString);

            eventHubSettings.processorHostOptions = new EventProcessorOptions();
            eventHubSettings.processorHostOptions.ExceptionReceived += WebSocketEventProcessor.ExceptionReceived;
            eventHubSettings.processorHostOptions.InitialOffsetProvider = (partitionId) => DateTime.UtcNow;
            //eventHubSettings.processorHostOptions.InitialOffsetProvider = partitionId =>
            //{
            //    return eventHubSettings.namespaceManager.GetEventHubPartition(eventHubSettings.client.Path, partitionId).LastEnqueuedOffset;
            //};

            Trace.TraceInformation("Registering EventProcessor for " + eventHubSettings.name);
            eventHubSettings.processorHost.RegisterEventProcessorAsync<WebSocketEventProcessor>(eventHubSettings.processorHostOptions).Wait();
        }

        private void GetAppSettings()
        {
            // Read settings for Devices Event Hub
            eventHubDevicesSettings.connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionStringDevices");
            eventHubDevicesSettings.name = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.EventHubDevices");
            eventHubDevicesSettings.storageConnectionString = CloudConfigurationManager.GetSetting("Microsoft.Storage.ConnectionString");
            eventHubDevicesSettings.namespaceManager = NamespaceManager.CreateFromConnectionString(CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString"));

            // Read settings for Alerts Event Hub
            eventHubAlertsSettings.connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionStringAlerts");
            eventHubAlertsSettings.name = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.EventHubAlerts");
            eventHubAlertsSettings.storageConnectionString = CloudConfigurationManager.GetSetting("Microsoft.Storage.ConnectionString");
            eventHubAlertsSettings.namespaceManager = NamespaceManager.CreateFromConnectionString(CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString"));

            if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
            {
                // Assume we are running local: use different consumer groups to avoid colliding with a cloud instance
                eventHubDevicesSettings.consumerGroup = "local";
                eventHubAlertsSettings.consumerGroup = "local";
            }
            else
            {
                eventHubDevicesSettings.consumerGroup = "WebSite";
                eventHubAlertsSettings.consumerGroup = "WebSite";
            }
        }

    }
}