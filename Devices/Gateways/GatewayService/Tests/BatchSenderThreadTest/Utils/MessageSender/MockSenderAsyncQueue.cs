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
    using Microsoft.ConnectTheDots.Gateway;
    using Microsoft.ConnectTheDots.Common.Threading;

    //--//

    internal class MockSenderAsyncQueue<T> : IMessageSender<T>
    {
        protected GatewayQueue<T> _SentMessagesQueue = new GatewayQueue<T>( );

        //--//

        public TaskWrapper SendMessage( T data )
        {
             _SentMessagesQueue.Push( data );

             return default( TaskWrapper );
        }

        public TaskWrapper SendSerialized( string jsonData )
        {
            throw new Exception( "Not implemented" );
        }

        public MockSenderMap<T> ToMockSenderMap( )
        {
            MockSenderMap<T> result = new MockSenderMap<T>( );

            int count = _SentMessagesQueue.Count;
            var tasks = new TaskWrapper[ count ];
            for( int processedCount = 0; processedCount < count; )
            {
                var popped = _SentMessagesQueue.TryPop( ).Result;
                if( popped.IsSuccess )
                {
                    result.SendMessage( popped.Result ).Wait( );
                    ++processedCount;
                }
            }

            return result;
        }

        public void Close( )
        {
        }
    }
}

