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
    using System;

    //--//

    public class SafeAction<TParam>
    {
        private readonly Action<TParam> _action;
        private readonly ILogger        _logger;

        //--//

        public SafeAction( Action<TParam> action, ILogger logger )
        {
            _action = action;
            _logger = SafeLogger.FromLogger( logger );
        }

        public void SafeInvoke( TParam obj )
        {
            try
            {
                _action( obj );
            }
            catch( Exception ex )
            {
                _logger.LogError("Exception in task: " + ex.StackTrace);
                _logger.LogError("Message in task: " + ex.Message);
            }
        }
    }

    public class SafeAction
    {
        private readonly Action         _action;
        private readonly ILogger        _logger;

        //--//

        public SafeAction( Action action, ILogger logger )
        {
            _action = action;
            _logger = SafeLogger.FromLogger( logger );
        }

        public void SafeInvoke( )
        {
            try
            {
                _action( );
            }
            catch( Exception ex )
            {
                _logger.LogError("Exception in task: " + ex.StackTrace);
                _logger.LogError("Message in task: " + ex.Message);
            }
        }
    }
}
