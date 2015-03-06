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

using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace CloudDeploy
{
    class Program
    {
        #region Configuration
        private static string devicesTopicName = "fbdevices";
        private static IEnumerable<string> devices = new List<string>() { "D1", "D2", "D3", "D4" };
        private static string deviceSharedAccessKeyName = "DeviceService";
        private static string webServiceAccessKeyName = "WebService";
        #endregion

        static void Main(string[] args)
        {
            DeployTopic(
                    CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString"), 
                    devicesTopicName, 
                    devices
            );

            Console.WriteLine("Press Enter to exit ...");
            Console.ReadLine();
        }

        /// <summary>
        /// Deploy topic and subscriptions, print connection informations
        /// </summary>
        static void DeployTopic(string serviceBusConnectionString, string topicName, IEnumerable<string> subscriptions)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(serviceBusConnectionString);

            Console.WriteLine("Creating topic {0}", topicName);
            try
            {

                if (namespaceManager.TopicExists(topicName))
                {
                    Console.WriteLine("Removing existed topic {0}", topicName);
                    namespaceManager.DeleteTopic(topicName);
                    Console.WriteLine("Creating new topic {0}", topicName);
                }

                var topicDescription = new TopicDescription(topicName);

                /* 
                 * Performance Improvements
                 * https://msdn.microsoft.com/en-us/library/azure/hh528527.aspx
                 */
                topicDescription.EnableExpress = true;
                // topicDescription.EnablePartitioning = true; // Will not work over AMQP

                topicDescription.Authorization.Add(new SharedAccessAuthorizationRule(deviceSharedAccessKeyName, new List<AccessRights>() { AccessRights.Listen }));
                topicDescription.Authorization.Add(new SharedAccessAuthorizationRule(webServiceAccessKeyName, new List<AccessRights>() { AccessRights.Send }));

                topicDescription = namespaceManager.CreateTopic(topicDescription);

                DeploySubscriptions(namespaceManager, topicDescription, subscriptions);

                #region Print connection informations

                string deviceSharedAccessKey = (topicDescription.Authorization.First(
                        (d) => String.Equals(d.KeyName, deviceSharedAccessKeyName, StringComparison.InvariantCultureIgnoreCase)) 
                                as SharedAccessAuthorizationRule
                    ).PrimaryKey;

                string webServiceSharedAccessKey = (topicDescription.Authorization.First(
                    (d) => String.Equals(d.KeyName, webServiceAccessKeyName, StringComparison.InvariantCultureIgnoreCase))
                            as SharedAccessAuthorizationRule
                ).PrimaryKey;

                string topicConnectionStringForDevice = new ServiceBusConnectionStringBuilder(serviceBusConnectionString)
                {
                    SharedAccessKeyName = deviceSharedAccessKeyName,
                    SharedAccessKey = deviceSharedAccessKey,
                    TransportType = TransportType.Amqp
                }.ToString();

                string topicConnectionStringForWebService = new ServiceBusConnectionStringBuilder(serviceBusConnectionString)
                {
                    SharedAccessKeyName = webServiceAccessKeyName,
                    SharedAccessKey = webServiceSharedAccessKey,
                    TransportType = TransportType.Amqp
                }.ToString();

                Console.WriteLine("Topic for Device:");
                Console.WriteLine(" SharedAccessKeyName:\t{0}", deviceSharedAccessKeyName);
                Console.WriteLine(" SharedAccessKey:\t{0}", deviceSharedAccessKey);
                Console.WriteLine(" Connection String:\t{0}", topicConnectionStringForDevice);
                Console.WriteLine("Topic for WebService");
                Console.WriteLine(" SharedAccessKeyName:\t{0}", webServiceAccessKeyName);
                Console.WriteLine(" SharedAccessKey:\t{0}", webServiceSharedAccessKey);
                Console.WriteLine(" Connection String:\t{0}", topicConnectionStringForWebService);

                #endregion
            }
            catch (MessagingException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Deploy subscriptions for devices
        /// </summary>
        /// <param name="namespaceManager">namespace manager for topic's service bus</param>
        /// <param name="topicDescription">topic description with topic's handle</param>
        static void DeploySubscriptions(NamespaceManager namespaceManager, TopicDescription topicDescription, IEnumerable<string> subscriptions)
        {

            foreach (var subscription in subscriptions)
            {
                Console.WriteLine("Creating subscription for {0}", subscription);
                try
                {

                    if (namespaceManager.SubscriptionExists(topicDescription.Path, subscription))
                    {
                        Console.WriteLine("Removing existed subscription {0}", subscription);
                        namespaceManager.DeleteSubscription(topicDescription.Path, subscription);
                        Console.WriteLine("Creating new subscription {0}", subscription);
                    }

                    var ruleDescription = new RuleDescription(new SqlFilter(String.Format("deviceId = '{0}' or deviceId = 'all'", subscription)));
                    var deviceSubscription = namespaceManager.CreateSubscription(topicDescription.Path, subscription, ruleDescription);

                    Console.WriteLine("Subscription path {0}/Subscriptions/{1}", topicDescription.Path, subscription);

                }
                catch (MessagingException e)
                {
                    Console.WriteLine("Failed to create subscription for {0}", subscription);
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}
