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

namespace Microsoft.ConnectTheDots.Gateway
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.ConnectTheDots.Common;
    using Microsoft.ConnectTheDots.Common.Threading;

    //--//

    public class GatewayQueue<T> : IAsyncQueue<T>
    {
        private readonly ConcurrentQueue<T> _Queue = new ConcurrentQueue<T>( );

        //--//

        async public void Push( T item )
        {
            _Queue.Enqueue( item );
        }

        public TaskWrapper<OperationStatus<T>> TryPop( )
        {
            Func<OperationStatus<T>> deque = ( ) =>
            {
                T returnedItem;

                bool isReturned = _Queue.TryDequeue( out returnedItem );

                if( isReturned )
                {
                    return OperationStatusFactory.CreateSuccess<T>( returnedItem );
                }

                return OperationStatusFactory.CreateError<T>( ErrorCode.NoDataReceived );
            };

            var sf = new SafeFunc<OperationStatus<T>>( deque, null );

            return TaskWrapper<OperationStatus<T>>.Run( () => sf.SafeInvoke() );
        }

        public int Count
        {
            get
            {
                return _Queue.Count;
            }
        }
    }
}
