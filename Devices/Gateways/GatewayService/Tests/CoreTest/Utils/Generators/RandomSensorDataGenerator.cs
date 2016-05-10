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
    using Microsoft.ConnectTheDots.Gateway;

    //--//

    public static class RandomSensorDataGenerator
    {
        private const int DEVICE_COUNT = 4;

        //--//

        private static readonly Random   _Random        = new Random( );
        private static readonly string[] _Guids         = new string[ DEVICE_COUNT ];
        private static readonly string[] _MeasureName   = new string[ DEVICE_COUNT ];
        private static readonly string[] _UnitOfMeasure = new string[ DEVICE_COUNT ];
        private static readonly string[] _DisplayName   = new string[ DEVICE_COUNT ];

        //--//

        static RandomSensorDataGenerator( )
        {
            for( int i = 0; i < DEVICE_COUNT; ++i )
            {
                int rint = i % 2;

                _Guids[ i ] = ( new Guid( _Random.Next( ), 0, 0, new byte[ 8 ] ) ).ToString( );
                _MeasureName[ i ] = rint == 0 ? "length" : "time";
                _UnitOfMeasure[ i ] = rint == 0 ? "m" : "s";
                _DisplayName[ i ] = "Sensor" + i + ( rint == 0 ? "m" : "s" );
            }
        }

        public static SensorDataContract Generate( )
        {
            int device = _Random.Next( ) % DEVICE_COUNT;

            SensorDataContract sensorData = new SensorDataContract
            {
                MeasureName = _MeasureName[ device ],
                UnitOfMeasure = _UnitOfMeasure[ device ],
                DisplayName = _DisplayName[ device ],
                Guid = _Guids[ device ],
                Value = _Random.Next( ) % 1000 - 500,
                Location = "here",
                Organization = "contoso",
                TimeCreated = DateTime.UtcNow
            };
            return sensorData;
        }
    }
}
