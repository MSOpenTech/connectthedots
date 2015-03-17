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

namespace Microsoft.ConnectTheDots.CloudDeploy.CreateWebConfig
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using Microsoft.ConnectTheDots.CloudDeploy.Common;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Management.ServiceBus;
    using Microsoft.WindowsAzure.Management.Storage;
    using Microsoft.WindowsAzure.Subscriptions.Models;

    //--//

    class Program
    {
        internal class CloudWebDeployInputs
        {
            public string NamePrefix;
            public string SBNamespace;
            public string Location;
            public string EventHubNameDevices;
            public string EventHubNameAlerts;
            public string StorageAccountName;
            public string WebSiteDirectory;
            public SubscriptionCloudCredentials Credentials;
        }
        
        // location of other projects
        bool Transform = false;

        //--//

        public bool GetInputs( out CloudWebDeployInputs result )
        {
            result = new CloudWebDeployInputs( );

            result.Credentials = AzureCredentialsProvider.GetUserSubscriptionCredentials( );
            if( result.Credentials == null )
            {
                result = null;
                return false;
            }

            Console.WriteLine( "Enter suggested namespace prefix: " );
            result.NamePrefix = Console.ReadLine( );
            if( string.IsNullOrEmpty( result.NamePrefix ) )
            {
                result = null;
                return false;
            }

            Console.WriteLine( "Enter suggested location: " );
            result.Location = Console.ReadLine( );
            if( string.IsNullOrEmpty( result.Location ) )
            {
                result.Location = "Central US";
            }

            result.SBNamespace = result.NamePrefix + "-ns";
            result.StorageAccountName = result.NamePrefix.ToLowerInvariant() + "storage";

            result.EventHubNameDevices = "ehdevices";
            result.EventHubNameAlerts = "ehalerts";

            result.WebSiteDirectory = "..\\..\\..\\..\\WebSite\\ConnectTheDotsWebSite"; // Default for running the tool from the bin/debug or bin/release directory (i.e within VS)
            return true;
        }

        public bool Run( )
        {
            CloudWebDeployInputs inputs = null;
            if( !GetInputs( out inputs ) )
            {
                return false;
            }

            if( !CreateWeb( inputs ) )
            {
                return false;
            }

            Console.ReadLine( );
            return true;
        }

        private bool CreateWeb( CloudWebDeployInputs inputs )
        {
            // Create Namespace
            ServiceBusManagementClient sbMgmt = new ServiceBusManagementClient( inputs.Credentials );

            var nsDescription = sbMgmt.Namespaces.GetNamespaceDescription( inputs.SBNamespace );
            string nsConnectionString = nsDescription.NamespaceDescriptions.First(
                ( d ) => String.Equals( d.AuthorizationType, "SharedAccessAuthorization" )
                ).ConnectionString;

            NamespaceManager nsManager = NamespaceManager.CreateFromConnectionString( nsConnectionString );

            EventHubDescription ehDevices = nsManager.GetEventHub( inputs.EventHubNameDevices );
            EventHubDescription ehAlerts = nsManager.GetEventHub( inputs.EventHubNameAlerts );

            StorageManagementClient stgMgmt = new StorageManagementClient( inputs.Credentials );
            var keyResponse = stgMgmt.StorageAccounts.GetKeys( inputs.StorageAccountName.ToLowerInvariant( ) );
            if ( keyResponse.StatusCode != System.Net.HttpStatusCode.OK )
            {
                Console.WriteLine( "Error retrieving access keys for storage account {0} in Location {1}: {2}",
                    inputs.StorageAccountName, inputs.Location, keyResponse.StatusCode );
                return false;
            }

            var storageKey = keyResponse.PrimaryKey;

            string ehDevicesWebSiteConnectionString = new ServiceBusConnectionStringBuilder( nsConnectionString )
            {
                SharedAccessKeyName = "WebSite",
                SharedAccessKey = ( ehDevices.Authorization.First( ( d )
                    => String.Equals( d.KeyName, "WebSite", StringComparison.InvariantCultureIgnoreCase) ) as SharedAccessAuthorizationRule ).PrimaryKey,
            }.ToString( );

            string ehAlertsWebSiteConnectionString = new ServiceBusConnectionStringBuilder( nsConnectionString )
            {
                SharedAccessKeyName = "WebSite",
                SharedAccessKey = ( ehAlerts.Authorization.First( ( d )
                    => String.Equals( d.KeyName, "WebSite", StringComparison.InvariantCultureIgnoreCase) ) as SharedAccessAuthorizationRule ).PrimaryKey,
            }.ToString( );

            // Write a new web.config template file
            var doc = new XmlDocument { PreserveWhitespace = true };

            var inputFileName = ( Transform ? "\\web.PublishTemplate.config" : "\\web.config" );
            var outputFileName = ( Transform ? String.Format("\\web.{0}.config", inputs.NamePrefix) : "\\web.config" );


            doc.Load( inputs.WebSiteDirectory + inputFileName );

            doc.SelectSingleNode(
                "/configuration/appSettings/add[@key='Microsoft.ServiceBus.EventHubDevices']/@value" ).Value
                = inputs.EventHubNameDevices;
            doc.SelectSingleNode( "/configuration/appSettings/add[@key='Microsoft.ServiceBus.EventHubAlerts']/@value" )
                .Value
                = inputs.EventHubNameAlerts;
            doc.SelectSingleNode(
                "/configuration/appSettings/add[@key='Microsoft.ServiceBus.ConnectionString']/@value" ).Value
                = nsConnectionString;
            doc.SelectSingleNode(
                "/configuration/appSettings/add[@key='Microsoft.ServiceBus.ConnectionStringDevices']/@value" ).Value
                = ehDevicesWebSiteConnectionString;
            doc.SelectSingleNode(
                "/configuration/appSettings/add[@key='Microsoft.ServiceBus.ConnectionStringAlerts']/@value" ).Value
                = ehAlertsWebSiteConnectionString;
            doc.SelectSingleNode( "/configuration/appSettings/add[@key='Microsoft.Storage.ConnectionString']/@value" )
                .Value =
                String.Format( "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", inputs.StorageAccountName,
                    storageKey );

            var outputFile = System.IO.Path.GetFullPath( inputs.WebSiteDirectory + outputFileName );

            doc.Save( outputFile );

            Console.WriteLine( "Web.Config saved to {0}", outputFile );
            return true;
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
                return 0;
            }
        }
    }
}
