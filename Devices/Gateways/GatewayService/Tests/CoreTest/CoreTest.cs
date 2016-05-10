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

//#define MOCK_SENDER

namespace Microsoft.ConnectTheDots.Test
{

    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using Microsoft.ConnectTheDots.Common;
    using Microsoft.ConnectTheDots.Gateway;
    using Microsoft.ConnectTheDots.Common.Threading;

    //--//

    public class CoreTest : ITest
    {
        public const int TEST_ITERATIONS = 5;
        public const int MAX_TEST_MESSAGES = 1000;

        //--//

        private const int STOP_TIMEOUT_MS = 5000; // ms

        //--//

        private readonly ILogger                                            _logger;
        private readonly AutoResetEvent                                     _completed;
        private readonly GatewayQueue<QueuedItem>                           _gatewayQueue;
        private readonly IMessageSender<SensorDataContract>                 _sender;
        private readonly BatchSenderThread<QueuedItem, SensorDataContract>  _batchSenderThread;
        private readonly Random                                             _rand;
        private          int                                                _totalMessagesSent;
        private          int                                                _totalMessagesToSend;

        private readonly Func<string, QueuedItem>                           _gatewayTransform;

        //--//

        public CoreTest( ILogger logger )
        {
            if( logger == null )
            {
                throw new ArgumentException( "Cannot run tests without logging" );
            }

            _completed = new AutoResetEvent( false );

            _logger = logger;

            _rand = new Random( );
            _totalMessagesSent = 0;
            _totalMessagesToSend = 0;
            _gatewayQueue = new GatewayQueue<QueuedItem>( );

#if MOCK_SENDER
            _sender = new MockSender<SensorDataContract>(this);
#else

            AMQPConfig amqpConfig = Loader.GetAMQPConfig( );

            _sender = new AMQPSender<SensorDataContract>(
                                                amqpConfig.AMQPSAddress,
                                                amqpConfig.EventHubName,
                                                amqpConfig.EventHubMessageSubject,
                                                amqpConfig.EventHubDeviceId,
                                                amqpConfig.EventHubDeviceDisplayName,
                                                _logger
                                                );
#endif

            _batchSenderThread = new BatchSenderThread<QueuedItem, SensorDataContract>(
                _gatewayQueue,
                _sender,
                dataTransform : null, 
                serializedData: m => ( m == null ) ? null : m.JsonData,
                logger        : _logger 
                );

            string gatewayIPAddressString = string.Empty;
            IPAddressHelper.GetIPAddressString( ref gatewayIPAddressString );

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
                    transform = ( m => DataTransforms.AddIPToLocation( transformPrev( m ), gatewayIPAddressString ) );
                }

                _gatewayTransform = ( m => DataTransforms.QueuedItemFromSensorDataContract( transform( m ) ) );
            }
        }

        public void Run( )
        {
            TestRepeatSend( );
            TestDeviceAdapter( );
        }

        public void TestRepeatSend( )
        {
            try
            {
                GatewayService service = PrepareGatewayService( );

                // Send a flurry of messages, repeat a few times

                // script message sequence
                int[] sequence = new int[ TEST_ITERATIONS ];
                for( int iteration = 0; iteration < TEST_ITERATIONS; ++iteration )
                {
                    int count = _rand.Next( MAX_TEST_MESSAGES );

                    sequence[ iteration ] = count;

                    _totalMessagesToSend += count;
                }

                const float mean = 39.6001f;
                const int range  = 10;

                Random rand = new Random( ( int )( DateTime.Now.Ticks >> 32 ) );

                // send the messages
                for( int iteration = 0; iteration < TEST_ITERATIONS; ++iteration )
                {
                    int count = sequence[ iteration ];

                    while( --count >= 0 )
                    {
                        //
                        // Build a message. 
                        // It will look something like this: 
                        // "{\"unitofmeasure\":\"%\",\"location\":\"Olivier's office\",\"measurename\":\"Humidity\",\"timecreated\":\"2/26/2015 12:50:29 AM\",\"organization\":\"MSOpenTech\",\"guid\":\"00000000-0000-0000-0000-000000000000\",\"value\":39.600000000000001,\"displayname\":\"NETMF\"}"
                        // 

                        bool add = ( rand.Next( ) % 2 ) == 0;
                        int variant = rand.Next( ) % range;
                        float value = mean;

                        StringBuilder sb = new StringBuilder( );
                        sb.Append( "{\"unitofmeasure\":\"%\",\"location\":\"Olivier's office\",\"measurename\":\"Humidity\"," );
                        sb.Append( "\"timecreated\":\"" );
                        sb.Append( DateTime.UtcNow.ToString( ) ); // this should look like "2015-02-25T23:07:47.159Z"
                        sb.Append( "\",\"organization\":\"MSOpenTech\",\"guid\":\"" );
                        sb.Append( new Guid( ).ToString( ) );
                        sb.Append( "\",\"value\":" );
                        sb.Append( ( value += add ? variant : -variant ).ToString( ) );
                        sb.Append( ",\"displayname\":\"NETMF\"}" );

                        string message = sb.ToString( );

                        service.Enqueue( message );

                        DataArrived( message );
                    }
                }

                Debug.Assert( _totalMessagesSent == _totalMessagesToSend );

                _completed.WaitOne( );

                _batchSenderThread.Stop( STOP_TIMEOUT_MS );
            }
            catch( Exception ex )
            {
                _logger.LogError( "exception caught: " + ex.StackTrace );
            }
            finally
            {
                _batchSenderThread.Stop( STOP_TIMEOUT_MS );
                _sender.Close( );
            }
        }

        public void TestDeviceAdapter( )
        {
            try
            {
                GatewayService service = PrepareGatewayService( );

                DeviceAdapterLoader dataIntakeLoader = new DeviceAdapterLoader( Loader.GetSources( ), Loader.GetEndpoints( ), _logger );

                _totalMessagesToSend += 5;

                dataIntakeLoader.StartAll( service.Enqueue, DataArrived );

                _completed.WaitOne( );

                dataIntakeLoader.StopAll( );

                _batchSenderThread.Stop( STOP_TIMEOUT_MS );
            }
            catch( Exception ex )
            {
                _logger.LogError( "exception caught: " + ex.StackTrace );
            }
            finally
            {
                _batchSenderThread.Stop( STOP_TIMEOUT_MS );
                _sender.Close( );
            }
        }

        public int TotalMessagesSent
        {
            get
            {
                return _totalMessagesSent;
            }
        }

        public int TotalMessagesToSend
        {
            get
            {
                return _totalMessagesToSend;
            }
        }

        public void Completed( )
        {
            _completed.Set( );

            Console.WriteLine( String.Format( "Test completed, {0} messages sent", _totalMessagesToSend ) );
        }

        private GatewayService PrepareGatewayService( )
        {
            _batchSenderThread.Start( );

            GatewayService service = new GatewayService(
                _gatewayQueue,
                _batchSenderThread,
                _gatewayTransform
            );

            service.Logger = _logger;
            service.OnDataInQueue += DataInQueue;

            _batchSenderThread.OnEventsBatchProcessed += EventBatchProcessed;

            return service;
        }

        protected void DataArrived( string data )
        {
            _totalMessagesSent++;
        }

        protected virtual void DataInQueue( QueuedItem data )
        {
            // LORENZO: test behaviours such as accumulating data an processing in batch
            // as it stands, we are processing every event as it comes in

            _batchSenderThread.Process( );
        }

        protected virtual void EventBatchProcessed( List<TaskWrapper> messages )
        {
            // LORENZO: test behaviours such as waiting for messages to be delivered or re-transmission

            if(messages == null || messages.Count == 0)
            {
                return;
            }

            foreach( TaskWrapper t in messages )
            {
                _logger.LogInfo( String.Format( "task {0} status is '{1}'", t.Id, t.Status.ToString( ) ) );
            }

            TaskWrapper.BatchWaitAll( ( ( List<TaskWrapper> )messages ).ToArray( ) );

            foreach( TaskWrapper t in messages )
            {
                _logger.LogInfo( String.Format( "task {0} status is '{1}'", t.Id, t.Status.ToString( ) ) );
            }
        }
    }
}

