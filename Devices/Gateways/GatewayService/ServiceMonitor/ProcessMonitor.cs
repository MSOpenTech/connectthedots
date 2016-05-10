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
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using Microsoft.ConnectTheDots.Common;

    //--//

    internal class ProcessMonitor : AbstractMonitor
    {
        private  string _executableName;
        private Process _target;
        private bool    _exit;

        //--//

        public ProcessMonitor( string executableName, ILogger logger ) : base( logger )
        {
            _executableName = executableName;

            // cannot find executable, we may still be able to run based if such executable 
            // is in the path or already running
            Directory.SetCurrentDirectory( Path.GetDirectoryName( Assembly.GetExecutingAssembly( ).Location ) );

            if( !File.Exists( _executableName ) )
            {
                // cannot find executable, we may still be able to run based on the target
                Logger.LogInfo( "Executable does not exists in the current directory" );
            }

            _exit = false;
        }

        public override bool Lock( string monitoringTarget )
        {
            //
            // try and open the monitored process
            //
            Process[] processes = Process.GetProcessesByName( monitoringTarget );

            if( processes.Length == 1 )
            {
                _target = processes[ 0 ];
            }

            if( processes.Length > 1 || processes.Length == 0 )
            {
                // if there is more than 1, kill them all and restart
                foreach( Process p in processes )
                {
                    Logger.LogInfo( String.Format( "Killing process {0}, PID: {1}", p.ProcessName, p.Id ) );

                    try
                    {
                        p.Kill( );
                    }
                    catch
                    {
                    }
                }

                // create new process
                _target = CreateProcess( _executableName );
            }

            return _target == null ? false : true;
        }

        public override void Monitor()
        {
            // monitoring loop
            while( _exit == false && _target != null )
            {
                Thread.Sleep( MonitoringInterval );

                Process t = _target;

                if( t.HasExited )
                {
                    Logger.LogInfo( String.Format( "Process '{0}' exited at time {1} with code {2}", _executableName, t.ExitTime, t.ExitCode ) );
                }
            }
        }

        public override void QuitMonitor()
        {
            _exit = true;
        }

        private Process CreateProcess( string monitoringExecutable )
        {
            Process p = new Process( );

            p.StartInfo = new ProcessStartInfo( monitoringExecutable );

            p.Exited += Restart;

            if( !p.Start( ) )
            {
                Logger.LogError( String.Format( "Process '{0}'could not be started", monitoringExecutable ) );

                return null;
            }

            return p;
        }

        private void Restart( object sender, EventArgs e )
        {
            _target = CreateProcess( _executableName );
        }
    }
}
