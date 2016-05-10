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
    using Microsoft.ConnectTheDots.Common.Threading;

    //--//

    internal class MockSender<T> : IMessageSender<T>
    {
        private static readonly int MAX_LAG = 1; // ms

        //--//

        protected readonly ITest  _test;
        protected readonly Random _rand;
        protected          int    _forSending;

        //--//

        internal MockSender( ITest test )
        {
            _forSending = 0;
            _test = test;
            _rand = new Random( );
        }

        public TaskWrapper SendMessage( T data )
        {
            SimulateSend( );
            return null;
        }

        public TaskWrapper SendSerialized( string jsonData )
        {
            SimulateSend( );
            return null;
        }

        public void Close( )
        {
        }

        private void SimulateSend( )
        {
            // Naive atetmpt to simulate network latency
            Thread.Sleep( _rand.Next( MAX_LAG ) );

            int totalMessagesSent = _test.TotalMessagesSent;

            // LORENZO: print all data and validate that they match the data sent
            if( Interlocked.Increment( ref _forSending ) == totalMessagesSent && totalMessagesSent >= _test.TotalMessagesToSend )
            {
                _test.Completed( );
            }
        }
    }
}

