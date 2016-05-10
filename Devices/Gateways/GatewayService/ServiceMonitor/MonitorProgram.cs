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

namespace Microsoft.ConnectTheDots.GatewayServiceMonitor
{
    using System;
    using System.Configuration;
    using Microsoft.ConnectTheDots.Common;

    //--//

    class MonitorProgram
    {
        private const int MONITORING_INTERVAL = 1000; // ms

        //--//

        static void Main( string[] args )
        {
            ILogger logger = SafeLogger.FromLogger( MonitorLogger.Instance );

            // try and open the GatewayService process
            string monitoringTarget = ConfigurationManager.AppSettings.Get( "MonitoringTarget" );
            string monitoringExecutable = ConfigurationManager.AppSettings.Get( "TargetExecutable" );
            string type = ConfigurationManager.AppSettings.Get( "TargetType" );

            if(String.IsNullOrEmpty( monitoringTarget ) || String.IsNullOrEmpty( monitoringExecutable ))
            {
                logger.LogError( "Error in configuration, cannot start monitoring" );

                return;
            }
            if(String.IsNullOrEmpty( type ))
            {
                logger.LogInfo( "No type specified, defaulting to 'process'" );

                type = AbstractMonitor.ProcessType;

                return;
            }

            AbstractMonitor monitor = null;

            switch(type)
            {
                case AbstractMonitor.ProcessType:
                    monitor = new ProcessMonitor( monitoringExecutable, logger );
                    break;
                case AbstractMonitor.ServiceType:
                    monitor = new ServiceMonitor( monitoringTarget, logger );
                    break;
                default:
                    throw new ArgumentException( String.Format( "Monitoring type {0} is unrecognized", type ) ); 
            }

            monitor.MonitoringInterval = MONITORING_INTERVAL;

            if(monitor.Lock( monitoringTarget ))
            {
                monitor.Monitor();
            }
        }

    }
}
