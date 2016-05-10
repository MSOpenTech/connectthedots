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

//#define DEBUG_LOG 


namespace Microsoft.ConnectTheDots.Gateway
{
    using System;
    using System.Threading;
    using Microsoft.ConnectTheDots.Common;
    using Microsoft.ConnectTheDots.Common.Threading;

    //--//

    public class GatewayService : IGatewayService
    {
        public delegate void DataInQueueEventHandler( QueuedItem data );

        //--//

        private readonly IAsyncQueue<QueuedItem>    _queue;
        private readonly EventProcessor             _eventProcessor;
        private readonly Func<string, QueuedItem>   _dataTransform;

        //--//

        public GatewayService( IAsyncQueue<QueuedItem> queue, EventProcessor processor, Func<string, QueuedItem> dataTransform = null )
        {
            if( queue == null || processor == null )
            {
                throw new ArgumentException( "task queue and event processor cannot be null" );
            }

            if( dataTransform != null )
            {
                _dataTransform = dataTransform;
            }
            else
            {
                _dataTransform = m => new QueuedItem
                    {
                        JsonData = m
                    };
            }

            _queue = queue;
            _eventProcessor = processor;
        }

        public ILogger Logger { get; set; }

        public int Enqueue( string jsonData )
        {
            if( jsonData != null )//not filling a queue by empty items
            {
                QueuedItem sensorData = _dataTransform( jsonData );

                if( sensorData != null )
                {
                    //TODO: we can check status of BatchSender and indicate error on request if needed
                    _queue.Push( sensorData );

                    DataInQueue( sensorData );
                }
            }

            return _queue.Count;
        }

        public event DataInQueueEventHandler OnDataInQueue;

        protected virtual void DataInQueue( QueuedItem data )
        {
            DataInQueueEventHandler newData = OnDataInQueue;

            if( newData != null )
            {
                var sh = new SafeAction<QueuedItem>( d => newData( d ), Logger );

                TaskWrapper.Run( ( ) => sh.SafeInvoke( data ) );
            }

            //
            // NO logging on production code, enable for diagnostic purposes for debugging 
            //
#if DEBUG_LOG
            LogMessageReceived( );
#endif
        }

#if DEBUG_LOG
        int _receivedMessages = 0;
        DateTime _start;
        private void LogMessageReceived( )
        {
            int sent = Interlocked.Increment( ref _receivedMessages );

            if( sent == 1 )
            {
                _start = DateTime.Now;
            }

            if( Interlocked.CompareExchange( ref _receivedMessages, 0, Constants.MessagesLoggingThreshold ) == Constants.MessagesLoggingThreshold )
            {
                DateTime now = DateTime.Now;

                TimeSpan elapsed = ( now - _start );

                _start = now;

                var sh = new SafeAction<String>( s => Logger.LogInfo( s ), Logger );

                TaskWrapper.Run( ( ) => sh.SafeInvoke(
                    String.Format( "GatewayService received {0} events succesfully in {1} ms ", Constants.MessagesLoggingThreshold, elapsed.TotalMilliseconds.ToString( ) ) ) );
            }
        }
#endif
    }
}