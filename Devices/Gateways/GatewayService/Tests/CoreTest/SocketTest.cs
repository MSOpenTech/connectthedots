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

namespace Microsoft.ConnectTheDots.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.ConnectTheDots.Gateway;
    using Microsoft.ConnectTheDots.Common;
    using Microsoft.ConnectTheDots.Common.Threading;

    //--//

    public class SocketTest : ITest
    {
        public const int TEST_ITERATIONS   = 5;
        public const int MAX_TEST_MESSAGES = 1000;

        //--//

        private const int STOP_TIMEOUT_MS = 5000; // ms

        //--//

        private readonly ILogger                                    _logger;
        private readonly AutoResetEvent                             _completed;
        private readonly GatewayQueue<QueuedItem>                   _gatewayQueue;
        private readonly IMessageSender<QueuedItem>                 _sender;
        private readonly BatchSenderThread<QueuedItem, QueuedItem>  _batchSenderThread;
        private          int                                        _totalMessagesSent;
        private          int                                        _totalMessagesToSend;

        //--//

        public SocketTest( ILogger logger )
        {
            if( logger == null )
            {
                throw new ArgumentException( "Cannot run tests without logging" );
            }

             _completed = new AutoResetEvent( false );

            _logger = logger;

            _totalMessagesSent = 0;
            _totalMessagesToSend = 0;
            _gatewayQueue = new GatewayQueue<QueuedItem>( );            

            _sender = new MockSender<QueuedItem>( this );
            //
            // To test with actual event hub, use the code below
            //  _sender = new AMQPSender<SensorDataContract>(Constants.AMQPSAddress, Constants.EventHubName, Constants.EventHubMessageSubject, Constants.EventHubDeviceId, Constants.EventHubDeviceDisplayName);
            //  ((AMQPSender<QueuedItem>)_Sender).Logger = new TestLogger();
            // 

            _batchSenderThread = new BatchSenderThread<QueuedItem, QueuedItem>( _gatewayQueue, _sender, m => m, null, _logger );
        }

        public void Run( )
        {
            TestRecieveMessagesFromSocketDevice( );
        }

        public void TestRecieveMessagesFromSocketDevice( )
        {
            const int MESSAGES_TO_SEND_BY_SOCKET = 5;
            try
            {
                IList<string> sources = Loader.GetSources( )
                    .Where( m => m.Contains( "Socket" ) ).ToList( );
                IList<SensorEndpoint> endpoints = Loader.GetEndpoints( )
                    .Where( m => m.Name.Contains( "Socket" ) ).ToList( );

                if( endpoints.Count == 0 )
                {
                    throw new Exception( "Need to specify local ip host for Socket interations " +
                                        "and name of endpoint should contain \"Socket\"" );
                }

                GatewayService service = PrepareGatewayService( );

                SensorEndpoint endpoint = endpoints.First( );
                SocketClientTestDevice device = new SocketClientTestDevice( _logger );
                device.Start( endpoint, MESSAGES_TO_SEND_BY_SOCKET );

                DeviceAdapterLoader dataIntakeLoader = new DeviceAdapterLoader(
                    sources,
                    endpoints,
                    _logger );

                _totalMessagesToSend += MESSAGES_TO_SEND_BY_SOCKET;

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

            GatewayService service = new GatewayService( _gatewayQueue, _batchSenderThread,
                m => DataTransforms.QueuedItemFromSensorDataContract(
                        DataTransforms.AddTimeCreated( DataTransforms.SensorDataContractFromString( m, _logger ) ), _logger ) );

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

