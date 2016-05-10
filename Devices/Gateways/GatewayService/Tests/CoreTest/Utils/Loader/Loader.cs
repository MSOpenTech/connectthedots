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
    using System.Collections.Generic;
    using System.Configuration;
    using Microsoft.ConnectTheDots.Common;
    using Microsoft.ConnectTheDots.Gateway;

    //--//

    internal class AMQPConfig
    {
        public string AMQPSAddress;
        public string EventHubName;
        public string EventHubMessageSubject;
        public string EventHubDeviceId;
        public string EventHubDeviceDisplayName;
    };

    internal class DataTransformsConfig
    {
        public bool AttachTime;
        public bool AttachIP;
    };

    internal static class Loader
    {
        internal static IList<String> GetSources( )
        {
            var dataIntakes = new List<String>( );

            DeviceAdapterConfigSection config = ConfigurationManager.GetSection( "dataIntakes" ) as DeviceAdapterConfigSection;

            if( config != null )
            {
                foreach( DeviceAdapterConfigInstanceElement e in config.Instances )
                {
                    dataIntakes.Add( e.AssemblyPath );
                }
            }

            return dataIntakes;
        }

        internal static IList<SensorEndpoint> GetEndpoints( )
        {
            var sensorEndpoints = new List<SensorEndpoint>( );

            SensorEndpointConfigSection sensorEndpointItems = ConfigurationManager.GetSection( "sensorEndpoints" )
                as SensorEndpointConfigSection;

            if( sensorEndpointItems != null )
            {
                foreach( SensorEndpointConfigInstanceElement sensorEndpointItem in sensorEndpointItems.Instances )
                {
                    sensorEndpoints.Add( new SensorEndpoint
                    {
                        Name = sensorEndpointItem.Name,
                        Host = sensorEndpointItem.Host,
                        Port = sensorEndpointItem.Port,
                    } );
                }
            }

            return sensorEndpoints;
        }

        internal static AMQPConfig GetAMQPConfig( )
        {
            AMQPServiceConfigSection section = ConfigurationManager.GetSection( "AMQPServiceConfig" ) as AMQPServiceConfigSection;
            AMQPConfig configData = null;

            if( section != null )
            {
                configData = new AMQPConfig
                {
                    AMQPSAddress = section.AMQPSAddress,
                    EventHubName = section.EventHubName,
                    EventHubMessageSubject = section.EventHubMessageSubject,
                    EventHubDeviceId = section.EventHubDeviceId,
                    EventHubDeviceDisplayName = section.EventHubDeviceDisplayName
                };
            }

            return configData;
        }

        internal static DataTransformsConfig GetDataTransformsConfig( )
        {
            DataTransformsConfigSection section = ConfigurationManager.GetSection( "dataTransformsConfig" ) as DataTransformsConfigSection;
            DataTransformsConfig configData;

            if (section != null)
            {
                configData = new DataTransformsConfig
                {
                    AttachTime = section.AttachTime,
                    AttachIP = section.AttachIP
                };
            }
            else
            {
                configData = new DataTransformsConfig
                {
                    AttachTime = true,
                    AttachIP = false
                };
            }

            return configData;
        }
    }

    internal class AMQPServiceConfigSection : ConfigurationSection
    {
        [ConfigurationProperty( "AMQPSAddress", DefaultValue = "AMQPSAddress", IsRequired = true )]
        public string AMQPSAddress
        {
            get
            {
                return ( string )this[ "AMQPSAddress" ];
            }
            set
            {
                this[ "AMQPSAddress" ] = value;
            }
        }

        [ConfigurationProperty( "EventHubName", DefaultValue = "EventHubName", IsRequired = true )]
        public string EventHubName
        {
            get
            {
                return ( string )this[ "EventHubName" ];
            }
            set
            {
                this[ "EventHubName" ] = value;
            }
        }

        [ConfigurationProperty( "EventHubMessageSubject", DefaultValue = "EventHubMessageSubject", IsRequired = true )]
        public string EventHubMessageSubject
        {
            get
            {
                return ( string )this[ "EventHubMessageSubject" ];
            }
            set
            {
                this[ "EventHubMessageSubject" ] = value;
            }
        }

        [ConfigurationProperty( "EventHubDeviceId", DefaultValue = "EventHubDeviceId", IsRequired = true )]
        public string EventHubDeviceId
        {
            get
            {
                return ( string )this[ "EventHubDeviceId" ];
            }
            set
            {
                this[ "EventHubDeviceId" ] = value;
            }
        }

        [ConfigurationProperty( "EventHubDeviceDisplayName", DefaultValue = "EventHubDeviceDisplayName", IsRequired = true )]
        public string EventHubDeviceDisplayName
        {
            get
            {
                return ( string )this[ "EventHubDeviceDisplayName" ];
            }
            set
            {
                this[ "EventHubDeviceDisplayName" ] = value;
            }
        }
    }

    internal class DataTransformsConfigSection : ConfigurationSection
    {
        [ConfigurationProperty( "AttachTime", DefaultValue = "false", IsRequired = true )]
        public bool AttachTime
        {
            get
            {
                return ( bool )this[ "AttachTime" ];
            }
            set
            {
                this[ "AttachTime" ] = value;
            }
        }

        [ConfigurationProperty("AttachIP", DefaultValue = "false", IsRequired = true)]
        public bool AttachIP
        {
            get
            {
                return ( bool )this[ "AttachIP" ];
            }
            set
            {
                this[ "AttachIP" ] = value;
            }
        }
    }

    public class DeviceAdapterConfigSection : ConfigurationSection
    {
        [ConfigurationProperty( "", IsRequired = true, IsDefaultCollection = true )]
        public DeviceAdapterConfigInstanceCollection Instances
        {
            get { return ( DeviceAdapterConfigInstanceCollection )this[ "" ]; }
            set { this[ "" ] = value; }
        }
    }

    public class DeviceAdapterConfigInstanceCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement( )
        {
            return new DeviceAdapterConfigInstanceElement( );
        }

        protected override object GetElementKey( ConfigurationElement element )
        {
            return ( ( DeviceAdapterConfigInstanceElement )element ).Name;
        }
    }

    public class DeviceAdapterConfigInstanceElement : ConfigurationElement
    {
        [ConfigurationProperty( "name", IsKey = true, IsRequired = true )]
        public string Name
        {
            get
            {
                return ( string )base[ "name" ];
            }
        }

        [ConfigurationProperty( "type", IsRequired = true )]
        public string TypeName
        {
            get
            {
                return ( string )base[ "type" ];
            }
        }

        [ConfigurationProperty( "assemblyPath", IsRequired = true )]
        public string AssemblyPath
        {
            get
            {
                return ( string )base[ "assemblyPath" ];
            }
        }
    }

    public class SensorEndpointConfigSection : ConfigurationSection
    {
        [ConfigurationProperty( "", IsRequired = true, IsDefaultCollection = true )]
        public SensorEndpointConfigInstanceCollection Instances
        {
            get { return ( SensorEndpointConfigInstanceCollection )this[ "" ]; }
            set { this[ "" ] = value; }
        }
    }

    public class SensorEndpointConfigInstanceCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement( )
        {
            return new SensorEndpointConfigInstanceElement( );
        }

        protected override object GetElementKey( ConfigurationElement element )
        {
            return ( ( SensorEndpointConfigInstanceElement )element ).Name;
        }
    }

    public class SensorEndpointConfigInstanceElement : ConfigurationElement
    {
        [ConfigurationProperty( "name", IsKey = true, IsRequired = true )]
        public string Name
        {
            get
            {
                return ( string )base[ "name" ];
            }
        }

        [ConfigurationProperty( "port", IsRequired = true )]
        public int Port
        {
            get
            {
                return ( int )base[ "port" ];
            }
        }

        [ConfigurationProperty( "host", IsRequired = true )]
        public string Host
        {
            get
            {
                return ( string )base[ "host" ];
            }
        }
    }
}