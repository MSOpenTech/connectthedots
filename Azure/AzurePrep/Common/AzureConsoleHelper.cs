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

namespace Microsoft.ConnectTheDots.CloudDeploy.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    //--//

    using Microsoft.Azure;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.WindowsAzure.Management.ServiceBus.Models;
    using Microsoft.WindowsAzure.Subscriptions.Models;

    //--//

    public class AzureConsoleHelper
    {
        public static SubscriptionCloudCredentials GetUserSubscriptionCredentials( )
        {
            Console.WriteLine( "Waiting for authentication result..." );
            TokenCloudCredentials toFoundSubscriptions = AzureCredentialsProvider.GetCredentialsByUserADAuth( );

            Console.WriteLine( "Retrieving a list of subscriptions..." );

            IList<SubscriptionListOperationResponse.Subscription> subscriptions =
                AzureCredentialsProvider.GetSubscriptionList( toFoundSubscriptions ).Result;

            if( !subscriptions.Any( ) )
            {
                Console.WriteLine( "No available subscriptions." );
                return null;
            }
            Console.WriteLine( "List of available subscriptions: " );
            int listSize = 1;
            foreach( var subscription in subscriptions )
            {
                Console.WriteLine( listSize + ": " + subscription.SubscriptionName );
                listSize++;
            }

            for( ;; )
            {
                Console.WriteLine( "Please select subscription number: " );

                string answer = Console.ReadLine( );
                int selection = 0;
                if( !int.TryParse( answer, out selection ) || selection >= listSize || selection < 1 )
                {
                    Console.WriteLine( "Incorrect subscription number." );
                    continue;
                }

                if( ConsoleHelper.Confirm( "Are you sure you want to use " + subscriptions[ selection - 1 ].SubscriptionName + " subscription?" ) )
                {
                    Console.WriteLine( "Requesting access to subscription..." );
                    TokenCloudCredentials result = AzureCredentialsProvider.GetCredentialsByUserADAuth(
                        subscriptions[ selection - 1 ].SubscriptionId,
                        subscriptions[ selection - 1 ].ActiveDirectoryTenantId
                    );

                    return result;
                }
            }
        }

        public static ServiceBusNamespace SelectNamespace( SubscriptionCloudCredentials credentials,
            string requestMessageText = "Please select namespace you want to use: ",
            string manualOption = null )
        {
            Console.WriteLine( "Retrieving a list of created namespaces..." );
            ServiceBusNamespace[] namespaces = AzureProvider.GetNamespaces( credentials );
            int namespaceCount = namespaces.Length;

            Console.WriteLine( "Created namespaces: " );

            for( int currentNamespace = 1; currentNamespace <= namespaceCount; ++currentNamespace )
            {
                Console.WriteLine( currentNamespace + ": " + 
                    namespaces[ currentNamespace - 1 ].Name + " (" + namespaces[currentNamespace - 1].Region+ ")" );
            }

            if( manualOption != null )
            {
                Console.WriteLine( "*: " + manualOption );
            }
            Console.WriteLine( "0: Exit without processing" );

            for( ;; )
            {
                Console.WriteLine( requestMessageText );

                string answer = Console.ReadLine( );

                if( answer.StartsWith( "*" ) )
                {
                    if( manualOption == null )
                    {
                        Console.WriteLine( "Incorrect namespace number." );
                        continue;    
                    }
                    
                    if( !ConsoleHelper.Confirm( "Are you sure you want to enter name prefix manually?" ) )
                    {
                        continue;
                    }

                    for( ;; )
                    {
                        Console.WriteLine( "Enter a name prefix: " );
                        string namePrefix = Console.ReadLine( );
                        if( string.IsNullOrEmpty( namePrefix ) )
                        {
                            Console.WriteLine( "Incorrect namespace prefix." );
                            continue;
                        }
                        if( ConsoleHelper.Confirm( "Are you sure you want to use namespace prefix " + namePrefix + "?" ) )
                        {
                            return new ServiceBusNamespace
                            {
                                Name = namePrefix
                            };
                        }
                    }
                }

                int selection;
                if( !int.TryParse( answer, out selection ) || selection > namespaceCount || selection < -1 )
                {
                    Console.WriteLine( "Incorrect namespace number." );
                    continue;
                }

                if( selection == 0 )
                {
                    return null;
                }

                if( ConsoleHelper.Confirm( "Are you sure you want to select " + namespaces[ selection - 1 ].Name + " namespace?" ) )
                {
                    return namespaces[ selection - 1 ];
                }
            }
        }

        public static EventHubDescription SelectEventHub( NamespaceManager nsManager, SubscriptionCloudCredentials credentials )
        {
            Console.WriteLine( "Retrieving a list of created Event Hubs..." );

            EventHubDescription[ ] eventHubs = nsManager.GetEventHubs( ).ToArray( );
            int eventHubCount = eventHubs.Length;

            Console.WriteLine( "Created Event Hubs: " );

            for( int currentNamespace = 1; currentNamespace <= eventHubCount; ++currentNamespace )
            {
                Console.WriteLine( currentNamespace + ": " + eventHubs[ currentNamespace - 1 ].Path );
            }

            for( ;; )
            {
                Console.WriteLine( "Please select Event Hub you want to use: " );

                string answer = Console.ReadLine( );

                int selection;
                if( !int.TryParse( answer, out selection ) || selection > eventHubCount || selection < -1 )
                {
                    Console.WriteLine( "Incorrect Event Hub number." );
                    continue;
                }

                if( ConsoleHelper.Confirm( "Are you sure you want to select " + eventHubs[ selection - 1 ].Path + " Event Hub?" ) )
                {
                    return eventHubs[ selection - 1 ];
                }
            }
        }
    }
}
