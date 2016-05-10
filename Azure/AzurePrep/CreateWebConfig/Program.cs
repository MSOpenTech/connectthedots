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
    using System.Linq;
    using System.Xml;

    using Microsoft.Azure;
    using Microsoft.WindowsAzure.Management.ServiceBus.Models;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.WindowsAzure.Management.ServiceBus;
    using Microsoft.WindowsAzure.Management.Storage;
    using Microsoft.WindowsAzure.Subscriptions.Models;

    //--//

    using Microsoft.ConnectTheDots.CloudDeploy.Common;

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
            //public string WebSiteDirectory;
            public SubscriptionCloudCredentials Credentials;

//            public bool Transform = false;
        }

        //--//

        public bool GetInputs( out CloudWebDeployInputs result )
        {
            result = new CloudWebDeployInputs( );

            result.Credentials = AzureConsoleHelper.GetUserSubscriptionCredentials( );
            if( result.Credentials == null )
            {
                result = null;
                return false;
            }

            ServiceBusNamespace selectedNamespace = AzureConsoleHelper.SelectNamespace( result.Credentials );
            if( selectedNamespace == null )
            {
                result = null;
                Console.WriteLine( "Quiting..." );
                return false;
            }
/*
            Console.WriteLine( "Need to select or not Transform flag." );
            Console.WriteLine( "If selected, the input and output file name will be \"web.config\" placed in Web project location." );
            Console.WriteLine( "Otherwise, input file name will be \"web.PublishTemplate.config\" and output - \"" +
                String.Format("web.{0}.config", result.NamePrefix) + "\".");

            for( ;; )
            {
                Console.WriteLine( "Do you want to use Transform flag? (y/n)" );

                string answer = Console.ReadLine( );
                string request = "not use";
                result.Transform = false;
                if( !string.IsNullOrEmpty( answer ) && answer.ToLower( ).StartsWith( "y" ) )
                {
                    result.Transform = true;
                    request = "use";
                }
                if( ConsoleHelper.Confirm( "Are you sure you want to " + request + " Transform flag?" ) )
                {
                    break;
                }
            }
*/
            result.NamePrefix = selectedNamespace.Name;
            if( result.NamePrefix.EndsWith( "-ns") )
            {
                result.NamePrefix = result.NamePrefix.Substring( 0, result.NamePrefix.Length - 3 );
            }

            result.SBNamespace = selectedNamespace.Name;
            result.StorageAccountName = result.NamePrefix.ToLowerInvariant( ) + "storage";

            result.Location = selectedNamespace.Region;

            result.EventHubNameDevices = "ehdevices";
            result.EventHubNameAlerts = "ehalerts";

            //result.WebSiteDirectory = "..\\..\\..\\..\\WebSite\\ConnectTheDotsWebSite"; // Default for running the tool from the bin/debug or bin/release directory (i.e within VS)
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

            Console.WriteLine( "Please hit enter to close." );
            Console.ReadLine( );
            return true;
        }

        

        private bool CreateWeb( CloudWebDeployInputs inputs )
        {
            Console.WriteLine( "Retrieving namespace metadata..." );
            // Create Namespace
            ServiceBusManagementClient sbMgmt = new ServiceBusManagementClient( inputs.Credentials );

            var nsDescription = sbMgmt.Namespaces.GetNamespaceDescription( inputs.SBNamespace );
            string nsConnectionString = nsDescription.NamespaceDescriptions.First(
                ( d ) => String.Equals( d.AuthorizationType, "SharedAccessAuthorization" )
                ).ConnectionString;

            NamespaceManager nsManager = NamespaceManager.CreateFromConnectionString( nsConnectionString );

            StorageManagementClient stgMgmt = new StorageManagementClient( inputs.Credentials );
            var keyResponse = stgMgmt.StorageAccounts.GetKeys( inputs.StorageAccountName.ToLowerInvariant( ) );
            if( keyResponse.StatusCode != System.Net.HttpStatusCode.OK )
            {
                Console.WriteLine( "Error retrieving access keys for storage account {0} in Location {1}: {2}",
                    inputs.StorageAccountName, inputs.Location, keyResponse.StatusCode );
                return false;
            }

            var storageKey = keyResponse.PrimaryKey;

            EventHubDescription ehDevices = nsManager.GetEventHub( inputs.EventHubNameDevices );
            string ehDevicesWebSiteConnectionString = new ServiceBusConnectionStringBuilder( nsConnectionString )
            {
                SharedAccessKeyName = "WebSite",
                SharedAccessKey = ( ehDevices.Authorization.First( ( d )
                    => String.Equals( d.KeyName, "WebSite", StringComparison.InvariantCultureIgnoreCase) ) as SharedAccessAuthorizationRule ).PrimaryKey,
            }.ToString( );

            string ehAlertsWebSiteConnectionString = string.Empty;
            try
            {
                EventHubDescription ehAlerts = nsManager.GetEventHub( inputs.EventHubNameAlerts );
                ehAlertsWebSiteConnectionString = new ServiceBusConnectionStringBuilder( nsConnectionString )
                {
                    SharedAccessKeyName = "WebSite",
                    SharedAccessKey = ( ehAlerts.Authorization.First( ( d )
                        => String.Equals( d.KeyName, "WebSite", StringComparison.InvariantCultureIgnoreCase ) ) as
                        SharedAccessAuthorizationRule ).PrimaryKey,
                }.ToString( );
            }
            catch
            {
            }

            Console.WriteLine( "Started processing..." );
            // Write a new web.config template file
            var doc = new XmlDocument { PreserveWhitespace = true };
            
            //var inputFileName = ( inputs.Transform ? "\\web.PublishTemplate.config" : "\\web.config" );
            string inputFileName = "web.PublishTemplate.config";
            //var outputFileName = ( inputs.Transform ? String.Format("\\web.{0}.config", inputs.NamePrefix) : "\\web.config" );
            string outputFileName = "web.config";

            //doc.Load( inputs.WebSiteDirectory + inputFileName );

            string inputFilePath = Environment.CurrentDirectory +@"\";
            Console.WriteLine("Opening and updating " + inputFilePath + inputFileName);

            doc.Load( inputFilePath + inputFileName );

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

            //var outputFile = System.IO.Path.GetFullPath( inputs.WebSiteDirectory + outputFileName );
            string outputFilePath = Environment.GetFolderPath( Environment.SpecialFolder.Desktop );
            //Console.WriteLine(outputFilePath);

            var outputFile = outputFilePath + @"\" + outputFileName;
            Console.WriteLine( "Writing updates to " + outputFile );

            doc.Save( outputFile );
            Console.WriteLine( " " );
            Console.WriteLine( "Web config saved to {0}", outputFile ); 
            Console.WriteLine( " " );
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
                Console.WriteLine( "Please hit enter to close." );
                Console.ReadLine( );
                return 0;
            }
        }
    }
}
