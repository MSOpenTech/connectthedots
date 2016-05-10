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

namespace Microsoft.ConnectTheDots.GatewayService
{
    using System;
    using System.Configuration;
    using System.ServiceModel.Web;
    using System.ServiceProcess;
    
    using _THREADING = System.Threading.Tasks;
    using Microsoft.ConnectTheDots.Gateway;
    using Microsoft.ConnectTheDots.Common;
    using Microsoft.ConnectTheDots.Common.Threading;


    //--//

    public class WindowsService : ServiceBase
    {
        private const int STOP_TIMEOUT_MS = 5000; // ms

        //--//

        private static WebServiceHost _webHost;

        //--//

        private readonly ILogger                        _logger;
        private readonly GatewayQueue<QueuedItem>       _gatewayQueue;
        private readonly AMQPSender<SensorDataContract> _AMPQSender;
        private readonly EventProcessor                 _batchSenderThread;
        private readonly DeviceAdapterLoader            _dataIntakeLoader;

        private readonly Func<string, QueuedItem>       _gatewayTransform;

        //--//

        private          string                         _gatewayIPAddressString = string.Empty;

        //--//

        public WindowsService( ILogger logger )
        {
            if( logger == null )
            {
                throw new ArgumentException( "Cannot run service without logging" );
            }

            _logger = logger;

            if( logger is TunableLogger )
            {
                TunableLogger.LoggingLevel loggingLevel = TunableLogger.LevelFromString( ConfigurationManager.AppSettings.Get( "LoggingLevel" ) );

                ( ( TunableLogger )logger ).Level = ( loggingLevel != TunableLogger.LoggingLevel.Undefined ) ? loggingLevel : TunableLogger.LoggingLevel.Errors;
            }

            try
            {
                _THREADING.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

                // Name the Windows Service
                ServiceName = Constants.WindowsServiceName;

                _gatewayQueue = new GatewayQueue<QueuedItem>( );
                AMQPConfig amqpConfig = Loader.GetAMQPConfig( );

                if( amqpConfig == null )
                {
                    _logger.LogError( "AMQP configuration is missing" );
                    return;
                }
                _AMPQSender = new AMQPSender<SensorDataContract>(
                                                    amqpConfig.AMQPSAddress,
                                                    amqpConfig.EventHubName,
                                                    amqpConfig.EventHubMessageSubject,
                                                    amqpConfig.EventHubDeviceId,
                                                    amqpConfig.EventHubDeviceDisplayName,
                                                    _logger
                                                    );
                
                _batchSenderThread = new BatchSenderThread<QueuedItem, SensorDataContract>(
                                                    _gatewayQueue,
                                                    _AMPQSender,
                                                    null,//m => DataTransforms.AddTimeCreated(DataTransforms.SensorDataContractFromQueuedItem(m, _Logger)),
                                                    new Func<QueuedItem, string>( m => m.JsonData ),
                                                    _logger );

                _dataIntakeLoader = new DeviceAdapterLoader( Loader.GetSources( ), Loader.GetEndpoints( ), _logger );

                TaskWrapper.Run( ( ) => IPAddressHelper.GetIPAddressString( ref _gatewayIPAddressString ) );

                DataTransformsConfig dataTransformsConfig = Loader.GetDataTransformsConfig( );
                if( dataTransformsConfig.AttachIP || dataTransformsConfig.AttachTime )
                {
                    Func<string, SensorDataContract> transform = ( m => DataTransforms.SensorDataContractFromString( m, _logger ) );

                    if( dataTransformsConfig.AttachTime )
                    {
                        var transformPrev = transform;
                        transform = ( m => DataTransforms.AddTimeCreated( transformPrev( m ) ) );
                    }

                    if( dataTransformsConfig.AttachTime )
                    {
                        var transformPrev = transform;
                        transform = ( m => DataTransforms.AddIPToLocation( transformPrev( m ), _gatewayIPAddressString ) );
                    }

                    _gatewayTransform = ( m => DataTransforms.QueuedItemFromSensorDataContract( transform( m ) ) );
                }
            }
            catch( Exception ex )
            {
                _logger.LogError( "Exception creating WindowsService: " + ex.Message );
            }
        }

        protected override void OnStart( string[] args )
        {
            _logger.LogInfo( "Service starting... " );

            if( _webHost != null )
            {
                _webHost.Close( );
            }

            _batchSenderThread.Start( );

            _webHost = new WebServiceHost( typeof( Microsoft.ConnectTheDots.Gateway.GatewayService ) );

            Gateway.GatewayService service = new Microsoft.ConnectTheDots.Gateway.GatewayService(
                _gatewayQueue,
                _batchSenderThread,
                _gatewayTransform
            );
            _webHost.Description.Behaviors.Add( new ServiceBehavior( ( ) => service ) );

            service.Logger = _logger;
            service.OnDataInQueue += OnData;

            _webHost.Open( );

            _dataIntakeLoader.StartAll( service.Enqueue );

            _logger.LogInfo( "...started" );
        }

        protected override void OnStop( )
        {
            _logger.LogInfo( "Service stopping... " );

            _dataIntakeLoader.StopAll( );

            // close web host first (message intake)
            if( _webHost != null )
            {
                _webHost.Close( );
                _webHost = null;
            }

            // shutdown processor (message processing)
            _batchSenderThread.Stop( STOP_TIMEOUT_MS );

            // shut down connection to event hub last
            if( _AMPQSender != null )
            {
                _AMPQSender.Close( );
            }

            _logger.LogInfo( "...stopped" );
        }

        protected virtual void OnData( QueuedItem data )
        {
            // LORENZO: test behaviours such as accumulating data an processing in batch
            _batchSenderThread.Process( );
        }

        

        static void Main( string[] args )
        {
            ILogger logger = null;

            try
            {
                logger = TunableLogger.FromLogger(
                    SafeLogger.FromLogger( EventLogger.Instance )
                    );

                Run( new WindowsService( logger ) );
            }
            catch( Exception ex )
            {
                if( logger != null )
                {
                    logger.LogError( ex.ToString( ) );
                }

                // just return...
            }
        }

        private void OnUnobservedTaskException( object sender, _THREADING.UnobservedTaskExceptionEventArgs e )
        {
            // prevent exception escalation
            e.SetObserved( );

            _logger.LogError( String.Format( "task Exception: '{0}'\r\nTrace:\r\n{1}", e.Exception.Message, e.Exception.StackTrace ) );
        }
    }
}
