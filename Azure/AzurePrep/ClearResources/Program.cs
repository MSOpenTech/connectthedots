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

namespace Microsoft.ConnectTheDots.CloudDeploy.ClearResources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    using Microsoft.Azure;
    using Microsoft.Azure.Management.StreamAnalytics;
    using Microsoft.Azure.Management.StreamAnalytics.Models;

    using Microsoft.WindowsAzure.Management.ServiceBus.Models;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.WindowsAzure.Management.ServiceBus;
    using Microsoft.WindowsAzure.Management.Storage;
    using Microsoft.WindowsAzure.Management.WebSites;
    using Microsoft.WindowsAzure.Subscriptions.Models;

    //--//

    using Microsoft.ConnectTheDots.CloudDeploy.Common;

    //--//

    class Program
    {
        internal class ClearResourcesInputs
        {
            public string NamePrefix;
            public bool NamespaceExists;
            public string SBNamespace;
            public string Location;
            public string StorageAccountName;
            //public string WebSiteDirectory;
            public SubscriptionCloudCredentials Credentials;
        }

        private static readonly LogBuffer _ConsoleBuffer = new LogBuffer(
            ( m ) => {
                Console.WriteLine( m );
            }
        );

        //--//

        public bool GetInputs( out ClearResourcesInputs result )
        {
            result = new ClearResourcesInputs( );

            result.Credentials = AzureConsoleHelper.GetUserSubscriptionCredentials( );
            if( result.Credentials == null )
            {
                result = null;
                return false;
            }

            ServiceBusNamespace selectedNamespace = AzureConsoleHelper.SelectNamespace( result.Credentials,
                "Please select namespace you want to clear resources for: ", "Enter manually (if Namespace was deleted but there are resources with the same name prefix)." );
            if( selectedNamespace == null )
            {
                result = null;
                Console.WriteLine( "Quiting..." );
                return false;
            }
            result.NamePrefix = selectedNamespace.Name;
            if( result.NamePrefix.EndsWith( "-ns") )
            {
                result.NamePrefix = result.NamePrefix.Substring( 0, result.NamePrefix.Length - 3 );
            }

            result.SBNamespace = selectedNamespace.Name;
            result.StorageAccountName = result.NamePrefix.ToLowerInvariant( ) + "storage";

            if( selectedNamespace.Region != null )
            {
                result.NamespaceExists = true;
                result.Location = selectedNamespace.Region;
            }

            return true;
        }

        public bool Run( )
        {
            ClearResourcesInputs inputs = null;
            if( !GetInputs( out inputs ) )
            {
                return false;
            }

            DeleteResources( inputs );

            Console.WriteLine( "Please hit enter to close." );
            Console.ReadLine( );
            return true;
        }

        private void DeleteResources( ClearResourcesInputs inputs )
        {        
            if( inputs.NamespaceExists )
            {
                _ConsoleBuffer.Add( "Connecting to Service Bus..." );
                ServiceBusManagementClient sbMgmt = new ServiceBusManagementClient( inputs.Credentials );

                bool deleteNamespace = ConsoleHelper.AskAndPerformAction(
                    "Do you want to delete whole namespace " + inputs.SBNamespace + " including all entities under it?",
                    "Are you sure you want to delete namespace " + inputs.SBNamespace + "?",
                    "Are you sure you do not want to delete namespace " + inputs.SBNamespace + "?",
                    ( ) =>
                    {
                        _ConsoleBuffer.Add( "Sending request to delete " + inputs.SBNamespace + " namespace..." );
                        AzureOperationResponse nsResponse = sbMgmt.Namespaces.Delete( inputs.SBNamespace );
                        if( nsResponse.StatusCode == HttpStatusCode.OK )
                        {
                            _ConsoleBuffer.Add( inputs.SBNamespace + " namespace was deleted." );
                        }
                    },
                    _ConsoleBuffer );

                //if we did not delete whole Namespace, maybe we want to delete some of its Event Hubs?
                if( !deleteNamespace )
                {
                    _ConsoleBuffer.Add( "Reading list of Event Hubs from " + inputs.SBNamespace + " namespace..." );

                    var nsDescription = sbMgmt.Namespaces.GetNamespaceDescription( inputs.SBNamespace );
                    var nsConnectionString = nsDescription.NamespaceDescriptions.First(
                        ( d ) => String.Equals( d.AuthorizationType, "SharedAccessAuthorization" )
                        ).ConnectionString;
                    var nsManager = NamespaceManager.CreateFromConnectionString( nsConnectionString );

                    var eventHubs = nsManager.GetEventHubs( );

                    foreach( var eventHubDescription in eventHubs )
                    {
                        EventHubDescription description = eventHubDescription;
                        ConsoleHelper.AskAndPerformAction(
                            "Do you want to delete Event Hub " + eventHubDescription.Path +
                            " including all messages under it?",
                            "Are you sure you want to delete Event Hub " + eventHubDescription.Path + "?",
                            "Are you sure you do not want to delete Event Hub " + eventHubDescription.Path + "?",
                            ( ) =>
                            {
                                _ConsoleBuffer.Add( "Sending request to delete " + description.Path + " Event Hub..." );
                                nsManager.DeleteEventHub( description.Path );
                                _ConsoleBuffer.Add( "Request to delete " + description.Path + " Event Hub was accepted." );
                            },
                            _ConsoleBuffer );
                    }
                }
            }

            //Deleting Storage
            _ConsoleBuffer.Add( "Reading list of Storage Accounts..." );
            StorageManagementClient stgMgmt = new StorageManagementClient( inputs.Credentials );
            HashSet<string> storageAccounts = new HashSet<string>( );
            foreach( var storageAccount in stgMgmt.StorageAccounts.List( ) )
            {
                storageAccounts.Add( storageAccount.Name );
            }

            int deletedCount = 0;
            if( storageAccounts.Contains( inputs.StorageAccountName ) )
            {
                ConsoleHelper.AskAndPerformAction(
                    "Do you want to delete " + inputs.StorageAccountName + " storage account?",
                    "Are you sure you want to delete " + inputs.StorageAccountName + " storage account?",
                    "Are you sure you do not want to delete " + inputs.StorageAccountName + " storage account?",
                    ( ) =>
                    {
                        _ConsoleBuffer.Add( "Sending request to delete " + inputs.StorageAccountName + " Storage account..." );
                        AzureOperationResponse resultStg = stgMgmt.StorageAccounts.Delete( inputs.StorageAccountName );
                        deletedCount += 1;
                        if( resultStg.StatusCode == System.Net.HttpStatusCode.OK )
                        {
                            _ConsoleBuffer.Add( "Storage account " + inputs.StorageAccountName + " was deleted." );
                        }
                    },
                    _ConsoleBuffer );
            }
            if( deletedCount == 0 )
            {
                _ConsoleBuffer.Add( "No related Storage account was detected." );
            }

            //Deleting Stream Analytics jobs
            _ConsoleBuffer.Add( "Reading list of Stream Analytics jobs..." );
            StreamAnalyticsManagementClient saMgmt = new StreamAnalyticsManagementClient( inputs.Credentials );
            JobListResponse jobListResponse = saMgmt.StreamingJobs.ListJobsInSubscription( new JobListParameters { PropertiesToExpand = string.Empty } );
            deletedCount = 0;
            foreach( var job in jobListResponse.Value )
            {
                if( job.Name.StartsWith( inputs.NamePrefix ) )
                {
                    Job jobToAsk = job;
                    ConsoleHelper.AskAndPerformAction(
                        "Do you want to delete Stream Analytics job " + job.Name + "?",
                        "Are you sure you want to delete Stream Analytics job  " + job.Name + "?",
                        "Are you sure you do not want to delete namespace " + job.Name + "?",
                        ( ) =>
                        {
                            //we need to figure out wat resourceGroup this job belongs to
                            //--//
                            const string resourceGroupPath = "/resourceGroups/";
                            const string providersPath = "/providers/";

                            int resourceGroupPathIndex = jobToAsk.Id.IndexOf( resourceGroupPath, System.StringComparison.Ordinal );
                            int providersPathIndex = jobToAsk.Id.IndexOf( providersPath, System.StringComparison.Ordinal );
                            int resourceGroupIdStartIndex = resourceGroupPathIndex + resourceGroupPath.Length;

                            string resourceGroup = jobToAsk.Id.Substring( resourceGroupIdStartIndex, providersPathIndex - resourceGroupIdStartIndex );
                            //--//

                            deletedCount += 1;
                            _ConsoleBuffer.Add( "Sending request to delete " + jobToAsk.Name + " Stream Analytics job..." );
                            LongRunningOperationResponse response = saMgmt.StreamingJobs.Delete( resourceGroup, jobToAsk.Name );
                            if( response.Status == OperationStatus.Succeeded )
                            {
                                _ConsoleBuffer.Add( "Stream Analytics job " + jobToAsk.Name + " was deleted." );
                            }
                        },
                        _ConsoleBuffer );
                }
            }
            if( deletedCount == 0 )
            {
                _ConsoleBuffer.Add( "No Stream Analytics job was deleted." );
            }
        }

        static int Main( string[] args )
        {
            var p = new Program( );

            try
            {
                bool result = p.Run( );
                return result ? 0 : 1;
            }
            catch ( Exception e )
            {
                Console.WriteLine( "Exception {0} while creating Azure resources at {1}", e.Message, e.StackTrace );
                Console.WriteLine( "Please hit enter to close." );
                Console.ReadLine( );
                return 0;
            }
        }
    }
}
