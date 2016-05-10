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
    using System.Diagnostics;
    using Microsoft.ConnectTheDots.Common;

    //--//

    public class TestLogger : ILogger
    {
        #region Singleton implementation

        private static readonly object     _syncRoot = new object( );
        private static          TestLogger _logger;

        internal static TestLogger Instance
        {
            get
            {
                if( _logger == null )
                {
                    lock( _syncRoot )
                    {
                        if( _logger == null )
                        {
                            _logger = new TestLogger( );
                        }
                    }
                }

                return _logger;
            }
        }

        private TestLogger( )
        {
        }

        #endregion

        public void Flush( )
        {
        }

        public void LogError( string logMessage )
        {
            Debug.WriteLine( "[ERROR]: " + logMessage );
        }

        public void LogInfo( string logMessage )
        {
            Debug.WriteLine( "[INFO ] : " + logMessage );
        }
    }
}
