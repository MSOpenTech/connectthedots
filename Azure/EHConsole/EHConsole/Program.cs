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

namespace Microsoft.ConnectTheDots.EHConsole
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    //--//
    
    using Microsoft.Azure;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.WindowsAzure.Management.ServiceBus;
    using Microsoft.WindowsAzure.Management.ServiceBus.Models;
    using Microsoft.WindowsAzure.Management.Storage;

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
            public string StorageAccountName;
            public SubscriptionCloudCredentials Credentials;
        }

        //--//

        private static readonly LogBuffer _ConsoleBuffer = new LogBuffer(
            ( m ) =>
            {
                Console.WriteLine( m );
            }
        );

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
            result.NamePrefix = selectedNamespace.Name;
            if( result.NamePrefix.EndsWith( "-ns" ) )
            {
                result.NamePrefix = result.NamePrefix.Substring( 0, result.NamePrefix.Length - 3 );
            }

            result.SBNamespace = selectedNamespace.Name;
            result.StorageAccountName = result.NamePrefix.ToLowerInvariant( ) + "storage";

            result.Location = selectedNamespace.Region;

            return true;
        }

        bool Run( )
        {
            CloudWebDeployInputs inputs = null;
            if( !GetInputs( out inputs ) )
            {
                return false;
            }

            Console.WriteLine( "Retrieving namespace metadata..." );
            // Create Namespace
            ServiceBusManagementClient sbMgmt = new ServiceBusManagementClient( inputs.Credentials );

            var nsDescription = sbMgmt.Namespaces.GetNamespaceDescription( inputs.SBNamespace );
            string nsConnectionString = nsDescription.NamespaceDescriptions.First(
                ( d ) => String.Equals( d.AuthorizationType, "SharedAccessAuthorization" )
                ).ConnectionString;

            NamespaceManager nsManager = NamespaceManager.CreateFromConnectionString( nsConnectionString );

            EventHubDescription ehDevices = AzureConsoleHelper.SelectEventHub( nsManager, inputs.Credentials );

            var serviceNamespace = inputs.SBNamespace;
            var hubName = ehDevices.Path;
            
            var sharedAccessAuthorizationRule = ehDevices.Authorization.FirstOrDefault( ( d )
                => d.Rights.Contains(AccessRights.Listen)) as SharedAccessAuthorizationRule;

            if( sharedAccessAuthorizationRule == null )
            {
                Console.WriteLine( "Cannot locate Authorization rule for WebSite key." );
                return false;
            }

            var receiverKeyName = sharedAccessAuthorizationRule.KeyName;
            var receiverKey = sharedAccessAuthorizationRule.PrimaryKey;
            //Console.WriteLine("Starting temperature processor with {0} partitions.", partitionCount);

            CancellationTokenSource cts = new CancellationTokenSource( );

            int closedReceivers = 0;
            AutoResetEvent receiversStopped = new AutoResetEvent( false );

            MessagingFactory factory = MessagingFactory.Create(ServiceBusEnvironment.CreateServiceUri( "sb", serviceNamespace, "" ),
                new MessagingFactorySettings
                {
                    TokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider( receiverKeyName, receiverKey ),
                    TransportType = TransportType.Amqp
                } );

            EventHubClient eventHubClient = factory.CreateEventHubClient( hubName );
            EventHubConsumerGroup eventHubConsumerGroup = eventHubClient.GetDefaultConsumerGroup( );

            int partitionCount = ehDevices.PartitionCount;

            for( int i = 0; i < partitionCount; i++ )
            {
                Task.Factory.StartNew( ( state ) =>
                {
                    try
                    {
                        _ConsoleBuffer.Add( string.Format( "Starting worker to process partition: {0}", state ) );

                        var receiver = eventHubConsumerGroup.CreateReceiver( state.ToString( ), DateTime.UtcNow );

                        _ConsoleBuffer.Add( string.Format( "Waiting for start receiving messages: {0} ...", state ) );

                        while( true )
                        {
                            // Receive could fail, I would need a retry policy etc...
                            var messages = receiver.Receive( 10 );
                            foreach( var message in messages )
                            {
                                //var eventBody = Newtonsoft.Json.JsonConvert.DeserializeObject<TemperatureEvent>(Encoding.Default.GetString(message.GetBytes()));
                                //Console.WriteLine("{0} [{1}] Temperature: {2}", DateTime.Now, message.PartitionKey, eventBody.Temperature);
                                _ConsoleBuffer.Add( message.PartitionKey + " sent message:" + Encoding.Default.GetString( message.GetBytes( ) ) );
                            }

                            if( cts.IsCancellationRequested )
                            {
                                Console.WriteLine( "Stopping: {0}", state );
                                receiver.Close( );
                                if( Interlocked.Increment( ref closedReceivers ) >= partitionCount )
                                {
                                    receiversStopped.Set();
                                }
                                break;
                            }
                        }
                    }
                    catch( Exception ex )
                    {
                        _ConsoleBuffer.Add( ex.Message );
                    }
                }, i );
            }

            Console.ReadLine( );
            cts.Cancel( );

            //waiting for all receivers to stop
            receiversStopped.WaitOne( );

            bool saveToFile;
            for( ;; )
            {
                Console.WriteLine( "Do you want to save received data to file? (y/n)" );

                string answer = Console.ReadLine( );
                string request = "do not";

                saveToFile = false;
                if( !string.IsNullOrEmpty( answer ) && answer.ToLower( ).StartsWith( "y" ) )
                {
                    saveToFile = true;
                    request = "";
                }
                if( ConsoleHelper.Confirm( "Are you sure you " + request + " want to save received data?" ) )
                {
                    break;
                }
            }
            if( saveToFile )
            {
                string fileName = inputs.SBNamespace + DateTime.UtcNow.ToString( "_d_MMM_h_mm" ) + ".log";
                string filePath = Environment.GetFolderPath( Environment.SpecialFolder.Desktop );
                string fileFullName = filePath + @"\" + fileName;
                if( _ConsoleBuffer.FlushToFile( fileFullName ) )
                {
                    Console.WriteLine( "Output was saved to your desktop, at " + fileFullName + " file." );
                }    
            }
            

            Console.WriteLine( "Wait for all receivers to close and then press ENTER." );
            Console.ReadLine( );

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
                Console.WriteLine( "Exception {0} at {1}", e.Message, e.StackTrace );
                return 0;
            }
        }
    }
}
