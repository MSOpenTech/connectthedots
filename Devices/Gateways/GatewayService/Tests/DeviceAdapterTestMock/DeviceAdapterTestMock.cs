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
    using Newtonsoft.Json;
    using Microsoft.ConnectTheDots.Common;
    using Microsoft.ConnectTheDots.Common.Threading;
    using Microsoft.ConnectTheDots.Gateway;

    //--//

    public class DeviceAdapterTestMock : DeviceAdapterAbstract
    {
        private const int SLEEP_TIME_MS    = 1000;
        private const int LOG_MESSAGE_RATE = 100;//should be positive

        //--//

        private Func<string, int> _enqueue;
        private bool              _doWorkSwitch;

        //--//

        public DeviceAdapterTestMock( ILogger logger )
            : base( logger )
        {
        }

        public override bool Start( Func<string, int> enqueue )
        {
            _enqueue = enqueue;

            _doWorkSwitch = true;

            var sh = new SafeAction<int>( ( t ) => TestRun( t ), _logger );

            TaskWrapper.Run( ( ) => sh.SafeInvoke( SLEEP_TIME_MS ) );

            return true;
        }

        public override bool Stop( )
        {
            _doWorkSwitch = false;

            return true;
        }

        public override bool SetEndpoint( SensorEndpoint endpoint = null )
        {
            //we don't need any endpoints for this Data Intake
            if( endpoint == null )
                return true;

            return false;
        }

        public void TestRun( int sleepTime )
        {
            int messagesSent = 0;
            do
            {
                SensorDataContract sensorData = RandomSensorDataGenerator.Generate( );

                string serializedData = JsonConvert.SerializeObject( sensorData );

                _enqueue( serializedData );

                if( ++messagesSent % LOG_MESSAGE_RATE == 0 )
                {
                    _logger.LogInfo( LOG_MESSAGE_RATE + " messages sent via DeviceAdapterTestMock." );
                }

                Thread.Sleep( sleepTime );

            } while( _doWorkSwitch );
        }
    }
}
