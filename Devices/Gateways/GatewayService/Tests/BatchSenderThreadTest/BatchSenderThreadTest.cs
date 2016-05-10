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
    using System.Threading;
    using Microsoft.ConnectTheDots.Gateway;
    using Microsoft.ConnectTheDots.Common;

    //--//

    public class BatchSenderThreadTest
    {
        private readonly ILogger _logger;
        private readonly Random  _random;

        //-//

        public BatchSenderThreadTest( ILogger logger )
        {
            if( logger == null )
            {
                throw new ArgumentException( "Cannot run tests without logging" );
            }
            
            _random = new Random( ( int )DateTime.Now.Ticks );

            _logger = logger;
        }

        public void Run( )
        {
            TestMessagesGoFromSourceToTarget( );
            TestMessagesGoFromSourceToTargetWithTwoBatchSenderThreads( );
        }
        private void TestMessagesGoFromSourceToTarget( )
        {
            MockSenderMap<int> targetMap = new MockSenderMap<int>( );
            
            MockSenderMap<int> sourceMap = new MockSenderMap<int>( );

            GatewayQueue<int> queue = new GatewayQueue<int>( );
            
            EventProcessor batchSenderThread = new BatchSenderThread<int, int>( queue, targetMap, m => m, null, _logger );
            
            batchSenderThread.Start( );

            const int batchesIterations = 100;
            const int maxQueuedItemCount = 20;
            const int waitForBatchThreadTimeMs = 22;

            for( int iteration = 0; iteration < batchesIterations; ++iteration )
            {
                int queuedItemCount = _random.Next( 1, maxQueuedItemCount );
                for( int count = 0; count < queuedItemCount; ++count )
                {
                    int itemToQueue = _random.Next( );

                    queue.Push( itemToQueue );

                    sourceMap.SendMessage( itemToQueue ).Wait( );
                }
                batchSenderThread.Process( );

                Thread.Sleep( waitForBatchThreadTimeMs );

                if( !targetMap.ContainsOthersItems( sourceMap )
                    || !sourceMap.ContainsOthersItems( targetMap ) )
                {
                    _logger.LogError( "Not processed message found" );
                    break;
                }
            }

            batchSenderThread.Stop( waitForBatchThreadTimeMs );
        }

        private void TestMessagesGoFromSourceToTargetWithTwoBatchSenderThreads( )
        {
            MockSenderAsyncQueue<int> targetQueue = new MockSenderAsyncQueue<int>( );
            MockSenderMap<int> sourceMap = new MockSenderMap<int>( );

            GatewayQueue<int> queue = new GatewayQueue<int>( );
            EventProcessor batchSenderThreadA = new BatchSenderThread<int, int>( queue, targetQueue, m => m, null, _logger );
            EventProcessor batchSenderThreadB = new BatchSenderThread<int, int>( queue, targetQueue, m => m, null, _logger );

            batchSenderThreadA.Start( );
            batchSenderThreadB.Start( );

            const int waitForBatchThreadTimeMs = 800;
            const int queuedItemCount = 400;

            for( int count = 0; count < queuedItemCount; ++count )
            {
                int itemToQueue = _random.Next( );

                queue.Push( itemToQueue );

                sourceMap.SendMessage( itemToQueue ).Wait( );
            }
            batchSenderThreadA.Process( );
            batchSenderThreadB.Process( );

            Thread.Sleep( waitForBatchThreadTimeMs );

            MockSenderMap<int> targetMap = targetQueue.ToMockSenderMap( );
            if( !targetMap.ContainsOthersItems( sourceMap )
                || !sourceMap.ContainsOthersItems( targetMap ) )
            {
                _logger.LogError( "Not processed message found" );
            }

            batchSenderThreadA.Stop( waitForBatchThreadTimeMs );
            batchSenderThreadB.Stop( waitForBatchThreadTimeMs );
        }        
    }
}
