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

namespace Microsoft.ConnectTheDots.Gateway
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Microsoft.ConnectTheDots.Common;

    //--//

    [DataContract]
    public class QueuedItem
    {
        [DataMember( Name = "serializedData" )]
        public string JsonData { get; set; }
    }

    public static class DataTransforms
    {
        public static QueuedItem QueuedItemFromSensorDataContract( SensorDataContract sensorData, ILogger logger = null )
        {
            if( sensorData == null )
            {
                return null;
            }

            QueuedItem result = null;
            try
            {
                result = new QueuedItem
                {
                    JsonData = JsonConvert.SerializeObject( sensorData )
                };
            }
            catch( Exception ex )
            {
                if( logger != null )
                {
                    logger.LogError( "Error on serialize item: " + ex.Message );
                }
            }

            return result;
        }
        public static SensorDataContract SensorDataContractFromString( string data, ILogger logger = null )
        {
            SensorDataContract result;
            try
            {
                result =
                    JsonConvert.DeserializeObject<SensorDataContract>( data );
            }
            catch( Exception ex )
            {
                result = null;
                //TODO: maybe better to add some metrics instead
                if( logger != null )
                {
                    logger.LogError( "Error on deserialize item: " + ex.Message );
                }
            }

            return result;
        }
        public static SensorDataContract SensorDataContractFromQueuedItem( QueuedItem data, ILogger logger = null )
        {
            if( data == null )
            {
                return null;
            }

            SensorDataContract result = SensorDataContractFromString( data.JsonData );
            return result;
        }

        public static SensorDataContract AddTimeCreated( SensorDataContract data )
        {
            if( data == null )
            {
                return null;
            }

            SensorDataContract result = data;
            if( result.TimeCreated == default( DateTime ) )
            {
                var creationTime = DateTime.UtcNow;
                result.TimeCreated = creationTime;
            }

            return result;
        }

        public static SensorDataContract AddIPToLocation( SensorDataContract data, string gatewayIPAddressString )
        {
            if( data == null )
            {
                return null;
            }

            SensorDataContract result = data;
            if( result.Location == null )
            {
                result.Location = "Unknown" + '\n';
            }
            else
            {
                result.Location = result.Location + '\n';
            }

            if( gatewayIPAddressString != null )
            {
                result.Location += gatewayIPAddressString;
            }

            return result;
        }
    }
}
