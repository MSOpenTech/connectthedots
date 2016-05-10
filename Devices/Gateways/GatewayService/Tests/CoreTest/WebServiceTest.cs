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
    using System.Net;
    using System.Diagnostics;
    using Microsoft.ConnectTheDots.Common;

    //--//

    public class WebServiceTest : ITest
    {
        public const int TEST_ITERATIONS = 100;
        public const int MAX_TEST_MESSAGES = 1000;

        //--//

        private const int MINUTES_TO_MILLISECONDS  = 60 * 1000;
        private const int STOP_TIMEOUT_MS          = 5000;                        // ms
        private const int MIN_WAIT_BEETWEEN_BURSTS = 5 * MINUTES_TO_MILLISECONDS; // 5 minutes in milliseconds

        //--//

        private readonly ILogger _logger;
        private readonly Random  _rand;
        private          string  _url;
        private          int     _totalMessagesSent;
        private          int     _totalMessagesToSend;

        //--//

        public WebServiceTest( string url, ILogger logger )
        {
            if( logger == null )
            {
                throw new ArgumentException( "Cannot run tests without logging" );
            }

            _logger = logger;

            _url = url;
            _rand = new Random( );
            _totalMessagesSent = 0;
            _totalMessagesToSend = 0;
        }

        public void Run( )
        {
            try
            {
                // Send a flurry of messages, repeat a few times
                for( int iteration = 0; iteration < TEST_ITERATIONS; ++iteration )
                {
                    int count = _rand.Next( MAX_TEST_MESSAGES );

                    while( --count >= 0 )
                    {
                        HttpWebRequest request = ( HttpWebRequest )WebRequest.Create( _url );
                        request.Method = "GET";

                        using( HttpWebResponse response = ( HttpWebResponse )request.GetResponse( ) )
                        {
                            if( response.StatusCode != HttpStatusCode.OK )
                            {
                                SignalError( response.StatusCode );
                            }
                            else
                            {
                                _totalMessagesSent++;
                            }
                        }
                    }

                    // sleep 5 to 10 minutes
                    int sleepMS = MIN_WAIT_BEETWEEN_BURSTS + _rand.Next( MIN_WAIT_BEETWEEN_BURSTS );

                    Console.WriteLine( String.Format( "Sent {0} messages, sleeping now for {1} minutes", _totalMessagesSent, sleepMS / MINUTES_TO_MILLISECONDS ) );

                    Thread.Sleep( sleepMS );
                }
            }
            catch( Exception ex )
            {
                _logger.LogError( "exception caught: " + ex.StackTrace );
            }
        }

        public void Completed( )
        {
            throw new NotImplementedException( );
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

        protected void SignalError( HttpStatusCode code )
        {
            _logger.LogError( "Response yielded error: " + code.ToString( ) );

            Debug.Assert( false );
        }

    }
}
