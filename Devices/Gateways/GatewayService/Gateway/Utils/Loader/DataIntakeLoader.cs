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

//#define DEBUG_LOG

namespace Microsoft.ConnectTheDots.Gateway
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    
    using Microsoft.ConnectTheDots.Common;

    //--//

    public class DeviceAdapterLoader
    {
        private static HashSet<SensorEndpoint> _SensorEndpoints = new HashSet<SensorEndpoint>( );

        //--//

        private readonly IList<DeviceAdapterAbstract> _dataIntakes;
        private readonly ILogger                   _logger;

        //--//

        protected Func<string, int> OnDataToEnqueue;

        //--//

        public DeviceAdapterLoader( IList<String> sources, IList<SensorEndpoint> endpoints, ILogger logger )
        {
            _logger = SafeLogger.FromLogger( logger );

#if DEBUG_LOG
            _logger.LogInfo( "Starting loading Data Intakes" );
#endif

            _dataIntakes = new List<DeviceAdapterAbstract>( );


            if( endpoints != null )
            {
                foreach( SensorEndpoint endpoint in endpoints )
                {
                    _SensorEndpoints.Add( endpoint );
                }
            }

#if DEBUG_LOG
            else
            {
                _logger.LogInfo( "No list of SensorEndpoints in configuration file, continuing..." );
            }
#endif

            //
            // enumerate all types with a IDeviceAdapter interface look in the current directory, in the 
            // running assembly directory and in the entry and executing assembly
            //
            var directories = new List<String>( );
            directories.Add( Directory.GetCurrentDirectory( ) );
            var di1 = Path.GetDirectoryName( Assembly.GetEntryAssembly( ).Location );
            var di2 = Path.GetDirectoryName( Assembly.GetCallingAssembly( ).Location );
            var di3 = Path.GetDirectoryName( Assembly.GetExecutingAssembly( ).Location );

            // do not duplicate search paths 
            if( !directories.Contains( di1 ) )
            {
                directories.Add( di1 );
            }
            if( !directories.Contains( di2 ) )
            {
                directories.Add( di2 );
            }
            if( !directories.Contains( di3 ) )
            {
                directories.Add( di3 );
            }

            //for each filename will store a flag - whether it was specified at config or not
            Dictionary<String, bool> sourcesToLoad = new Dictionary<string, bool>( );
            if( sources != null && sources.Any( ) )
            {
                foreach( var filename in sources )
                {
                    sourcesToLoad.Add( filename, true );
                }
            }
            else
            {
#if DEBUG_LOG
                _logger.LogInfo( "No list of DeviceAdapters in configuration file, continuing..." );
#endif
                foreach( string directory in directories )
                {
                    //Dinar: dont want to try all windows/system32 directory for now
                    if( !directory.ToLowerInvariant( ).Contains( "system32" ) )
                    {
                        foreach( string filename in Directory.GetFiles( directory ) )
                        {
                            //false flag - file was not specified at config
                            sourcesToLoad.Add( filename, false );
                        }
                    }
                }
            }

            // try and load assemblies from path 
            var nameTypeDict = new Dictionary<string, Type>( );
            string notLoadedSpecifiedSources = "";
            string notLoadedNotSpecifiedSources = "";

            foreach( KeyValuePair<string, bool> source in sourcesToLoad )
            {
                string fileName = source.Key;
                try
                {
                    Assembly assm = null;
                    // try path from config first
                    if( File.Exists( fileName ) )
                    {
                        assm = Assembly.LoadFrom( fileName );

                        // remember this directory as a potential source
                        var lastDir = Path.GetDirectoryName( fileName );
                        if( !directories.Contains( lastDir ) )
                        {
                            directories.Add( lastDir );
                        }
                    }
                    else
                    {
                        // try the other directories
                        foreach( var d in directories )
                        {
                            var assmName = Path.GetFileName( fileName );

                            // try again
                            string assmPath = Path.Combine( d, assmName );
                            if( File.Exists( assmPath ) )
                            {
                                assm = Assembly.LoadFrom( assmPath );
                            }
                        }

                        if( assm == null )
                        {
                            // Log that we did not load our data sources correctly
                            if( source.Value )
                            {
                                notLoadedSpecifiedSources += fileName;
                            }
                            else
                            {
                                notLoadedNotSpecifiedSources += fileName;
                            }
                            notLoadedSpecifiedSources += "; ";
                            continue;
                        }
                    }

                    Debug.Assert( assm != null );

                    foreach( Type t in assm.GetExportedTypes( ) )
                    {
                        //Get all classes that implement the required interface
                        if( t.GetInterface( "IDeviceAdapter", false ) != null )
                        {
#if DEBUG_LOG
                            _logger.LogInfo( "IDeviceAdapter assembly loaded: " + t.Name );
#endif

                            nameTypeDict.Add( t.Name, t ); //Add to Dictonary
                        }
                    }
                }
                catch( Exception )
                {
                    //dont want to stop loading another modules if one fails
                    if( source.Value )
                    {
                        notLoadedSpecifiedSources += fileName;
                    }
                    else
                    {
                        notLoadedNotSpecifiedSources += fileName;
                    }
                    notLoadedNotSpecifiedSources += "; ";
                }
            }

            if( notLoadedSpecifiedSources.Length > 0 )
            {
                _logger.LogError( String.Format( "Following Data Intakes were specificied, but could not be loaded: {0}", notLoadedSpecifiedSources ) );
            }
            if( notLoadedNotSpecifiedSources.Length > 0 )
            {
                _logger.LogError( String.Format( "Following files are not specificied, and could not be loaded as Data Intakes: {0}", notLoadedNotSpecifiedSources ) );
            }

            foreach( KeyValuePair<string, Type> t in nameTypeDict )
            {
                try
                {
                    DeviceAdapterAbstract di = ( DeviceAdapterAbstract )Activator.CreateInstance( t.Value, new object[] { _logger } );

                    if( di != null )
                    {
#if DEBUG_LOG
                        _logger.LogInfo( "IDeviceAdapter instance created: " + t.Key );
#endif

                        //adding instance without endpoint if acceptable
                        if( di.SetEndpoint( ) )
                        {
                            _dataIntakes.Add( di );
                        }

                        foreach( SensorEndpoint sensorEndpoint in _SensorEndpoints )
                        {
                            DeviceAdapterAbstract diWithEndpoint = ( DeviceAdapterAbstract )Activator.CreateInstance( t.Value, new object[] { _logger } );
                            if( diWithEndpoint.SetEndpoint( sensorEndpoint ) )
                            {
                                _dataIntakes.Add( diWithEndpoint );
                            }
                        }
                    }
                }
                catch( Exception ex )
                {
                    // dont want to stop creating another instances if one fails
                    _logger.LogError( String.Format( "Exception on Creating DeviceAdapter Instance \"{0}\": {1}", t.Key, ex.Message ) );
                }
            }
        }

        public IList<IDeviceAdapter> Intakes
        {
            get
            {
                return ( IList<IDeviceAdapter> )_dataIntakes;
            }
        }

        public void StartAll( Func<string, int> enqueue, DataArrivalEventHandler onDataArrival = null )
        {
            foreach( DeviceAdapterAbstract dataIntake in _dataIntakes )
            {
                try
                {
                    if( onDataArrival != null )
                    {
                        OnDataToEnqueue =
                            data =>
                            {
                                onDataArrival( data );

                                return enqueue( data );
                            };
                    }
                    else
                    {
                        OnDataToEnqueue = enqueue;
                    }

                    dataIntake.Start( OnDataToEnqueue );
                }
                catch( Exception ex )
                {
                    _logger.LogError( "Exception on Starting DeviceAdapter: " + ex.StackTrace );

                    // catch all other exceptions
                }
            }
        }

        public void StopAll( )
        {
            foreach( DeviceAdapterAbstract dataIntake in _dataIntakes )
            {
                try
                {
                    dataIntake.Stop( );
                }
                catch( Exception ex )
                {
                    _logger.LogError( ex.StackTrace );

                    // catch all exceptions
                }
            }
        }
    }
}
