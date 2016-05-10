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
    using Microsoft.ConnectTheDots.Common;
    using Microsoft.ConnectTheDots.Gateway;
    using Microsoft.ConnectTheDots.Common.Threading;

    //--//

    internal class MockSenderMap<T> : IMessageSender<T>
    {
        protected SortedDictionary<T, int> _sentMessages = new SortedDictionary<T, int>( );

        //--//

        public TaskWrapper SendMessage( T data )
        {
            Action<T> send = ( d ) =>
            {
                lock( _sentMessages )
                {
                    if( _sentMessages.ContainsKey( d ) )
                        _sentMessages[ d ]++;
                    else _sentMessages.Add( d, 1 );
                }
            };

            var sh = new SafeAction<T>( ( d ) => send( d ), null );

            return TaskWrapper.Run( ( ) => sh.SafeInvoke( data ) );
        }

        public TaskWrapper SendSerialized( string jsonData )
        {
            throw new Exception( "Not implemented" );
        }

        public bool Contains( T data )
        {
            lock( _sentMessages )
            {
                return _sentMessages.ContainsKey( data );
            }
        }

        public bool ContainsOthersItems( MockSenderMap<T> other )
        {
            lock( _sentMessages )
            {
                if( other._sentMessages.Any( key => !_sentMessages.Contains( key ) ) )
                {
                    return false;
                }
                return true;
            }
        }

        public void Close( )
        {
        }
    }
}

