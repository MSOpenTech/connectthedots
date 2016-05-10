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

namespace Microsoft.ConnectTheDots.Common
{

    public class SafeLogger : ILogger
    {
        protected readonly ILogger _logger;

        //--//

        protected SafeLogger( ILogger logger )
        {
            _logger = logger;
        }

        static public SafeLogger FromLogger( ILogger logger )
        {
            if( logger is SafeLogger )
            {
                return ( SafeLogger )logger;
            }

            return new SafeLogger( logger );
        }

        public void Flush( )
        {
            if( _logger != null )
            {
                _logger.Flush( );
            }
        }

        public void LogError( string logMessage )
        {
            if( _logger != null )
            {
                _logger.LogError( logMessage );
            }
        }

        public void LogInfo( string logMessage )
        {
            if( _logger != null )
            {
                _logger.LogInfo( logMessage );
            }
        }
    }
}
