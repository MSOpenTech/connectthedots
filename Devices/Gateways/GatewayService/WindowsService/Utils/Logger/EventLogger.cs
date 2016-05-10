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

namespace Microsoft.ConnectTheDots.GatewayService
{
    using System.ComponentModel;
    using System.Diagnostics;
    using Gateway;
    using Microsoft.ConnectTheDots.Common;

    //--//

    public class WindowsEventLogger : ILogger
    {
        #region Singleton implementation

        private static readonly object  _syncRoot = new object( );

        //--//

        private static ILogger          _WindowsEventLoggerInstance;
        private static EventLog         _eventLog;

        //--//
        
        public static ILogger Instance
        {
            get
            {
                if( _WindowsEventLoggerInstance == null )
                {
                    lock( _syncRoot )
                    {
                        if( _WindowsEventLoggerInstance == null )
                        {
                            _WindowsEventLoggerInstance = new WindowsEventLogger( );
                        }
                    }
                }

                return _WindowsEventLoggerInstance;
            }
        }

        private WindowsEventLogger( )
        {
            _eventLog = new EventLog
            {
                Source = Constants.WindowsServiceName,
                Log = "Application"
            };

            ( ( ISupportInitialize )( _eventLog ) ).BeginInit( );
            if( !EventLog.SourceExists( _eventLog.Source ) )
            {
                EventLog.CreateEventSource( _eventLog.Source, _eventLog.Log );
            }
            ( ( ISupportInitialize )( _eventLog ) ).EndInit( );
        }

        #endregion

        public void Flush( )
        {
        }

        public void LogError( string logMessage )
        {
            _eventLog.WriteEntry( logMessage, EventLogEntryType.Error );
        }

        public void LogInfo( string logMessage )
        {
            _eventLog.WriteEntry( logMessage, EventLogEntryType.Information );
        }
    }

    public static class EventLogger
    {
        private static readonly object  _syncRoot = new object( );

        //--//

        private static ILogger _eventLogger;

        //--//

        public static ILogger Instance
        {
            get
            {
                if( _eventLogger == null )
                {
                    lock( _syncRoot )
                    {
                        if( _eventLogger == null )
                        {
                            _eventLogger = Platform.IsMono ? NLogEventLogger.Instance : WindowsEventLogger.Instance;
                        }
                    }
                }

                return _eventLogger;
            }
        }
    }
}
