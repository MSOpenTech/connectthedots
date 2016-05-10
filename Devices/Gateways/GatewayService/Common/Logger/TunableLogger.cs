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

    public class TunableLogger : ILogger
    {
        public enum LoggingLevel
        {
            Disabled = 0,
            Errors = 1,
            Verbose = 2,
            Undefined = 3,
        }

        //--//

        private LoggingLevel _level;

        //--//

        protected readonly ILogger _Logger;

        //--//

        protected TunableLogger( ILogger logger )
        {
            _Logger = logger;
            _level = LoggingLevel.Errors;
        }

        //--//

        public static LoggingLevel LevelFromString( string value )
        {
            if( !String.IsNullOrEmpty( value ) )
            {
                if( value == LoggingLevel.Disabled.ToString( ) )
                {
                    return LoggingLevel.Disabled;
                }
                if( value == LoggingLevel.Errors.ToString( ) )
                {
                    return LoggingLevel.Errors;
                }
                if( value == LoggingLevel.Verbose.ToString( ) )
                {
                    return LoggingLevel.Verbose;
                }
            }

            return LoggingLevel.Undefined;
        }

        public static TunableLogger FromLogger( ILogger logger )
        {
            if( logger is TunableLogger )
            {
                return ( TunableLogger )logger;
            }

            return new TunableLogger( logger );
        }

        //--//

        public void Flush( )
        {
            if (_Logger != null)
            {
                _Logger.Flush( );
            }
        }

        public void LogError( string logMessage )
        {
            if( _level >= LoggingLevel.Errors )
            {
                _Logger.LogError( logMessage );
            }
        }

        public void LogInfo( string logMessage )
        {
            if( _level >= LoggingLevel.Verbose )
            {
                _Logger.LogInfo( logMessage );
            }
        }

        public LoggingLevel Level
        {
            get
            {
                return _level;
            }
            set
            {
                _level = value;
            }
        }
    }
}
