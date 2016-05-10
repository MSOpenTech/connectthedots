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
    using System.IO;
    using System.Reflection;
    using System.ServiceProcess;
    using System.Threading;
    using Microsoft.ConnectTheDots.Common;

    //--//

    internal class ServiceMonitor : AbstractMonitor
    {
        private string            _serviceName;
        private ServiceController _target;
        private bool              _exit;

        //--//

        public ServiceMonitor( string serviceName, ILogger logger ) : base( logger )
        {
            _serviceName = serviceName;

            // check that the file we are supposed to launch actually exists 
            Directory.SetCurrentDirectory( Path.GetDirectoryName( Assembly.GetExecutingAssembly( ).Location ) );

            ServiceController[] svcs = ServiceController.GetServices( );

            foreach( ServiceController svc in svcs )
            {
                if( svc.DisplayName == serviceName )
                {
                    _target = svc;
                }
            }

            if( _target == null )
            {
                Logger.LogInfo( String.Format( "Service '{0}' is not installed", serviceName ) );
            }

            _exit = false;
        }

        public override bool Lock( string monitoringTarget )
        {
            if( _target == null )
            {
                return false;
            }

            if( _target.Status == ServiceControllerStatus.Stopped || _target.Status == ServiceControllerStatus.StopPending )
            {
                // create new process
                _target.Start( );
            }

            return true;
        }

        public override void Monitor()
        {
            // monitoring loop
            while( _exit == false )
            {
                Thread.Sleep( MonitoringInterval );

                if( _target != null )
                {
                    _target.Refresh( );

                    if( _target.Status == ServiceControllerStatus.Stopped )
                    {
                        Logger.LogInfo( String.Format( "Service '{0}' stopped at time {1} or earlier", _serviceName, DateTime.Now.ToString( ) ) );

                        Restart( );
                    }
                }
            }
        }

        public override void QuitMonitor()
        {
            _exit = true;
        }

        private void Restart( )
        {
            _target.Start( );
        }
    }
}
