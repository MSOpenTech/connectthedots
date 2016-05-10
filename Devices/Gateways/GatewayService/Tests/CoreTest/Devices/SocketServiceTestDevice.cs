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
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using Newtonsoft.Json;
    using Microsoft.ConnectTheDots.Common;
    using Microsoft.ConnectTheDots.Gateway;
    using Microsoft.ConnectTheDots.Common.Threading;

    //--//

    public class SocketServiceTestDevice
    {
        private const int SLEEP_TIME_MS = 1000;

        //--//

        private readonly ILogger _logger;

        //--//

        public SocketServiceTestDevice( ILogger logger )
        {
            if( logger == null )
            {
                throw new ArgumentException( "Please provide logger to SocketServiceDevice" );
            }

            _logger = logger;
        }


        public void Start( SensorEndpoint endpoint )
        {
            var sh = new SafeAction<SensorEndpoint>( e => RunSocketServer( e ), _logger );

            TaskWrapper.Run( ( ) => sh.SafeInvoke( endpoint ) );
        }

        public void RunSocketServer( SensorEndpoint endpoint )
        {
            IPAddress ipAddress;
            if( !IPAddress.TryParse( endpoint.Host, out ipAddress ) )
                return;

            TcpListener serverSocket = new TcpListener( ipAddress, endpoint.Port );
            serverSocket.Start( );

            TcpClient clientSocket = serverSocket.AcceptTcpClient( );

            try
            {
                for( ; ; )
                {
                    NetworkStream networkStream = clientSocket.GetStream( );

                    //byte[] bytesFrom = new byte[10025];
                    //networkStream.Read(bytesFrom, 0, clientSocket.ReceiveBufferSize);

                    //string dataFromClient = Encoding.ASCII.GetString(bytesFrom);
                    //dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));

                    SensorDataContract sensorData = RandomSensorDataGenerator.Generate( );
                    string serializedData = JsonConvert.SerializeObject( sensorData );

                    Byte[] sendBytes = Encoding.ASCII.GetBytes( "<" + serializedData + ">" );

                    networkStream.Write( sendBytes, 0, sendBytes.Length );
                    networkStream.Flush( );

                    Thread.Sleep( SLEEP_TIME_MS );
                }
            }
            catch( Exception ex )
            {
                _logger.LogError( ex.ToString( ) );
            }

            try
            {
                serverSocket.Stop( );
            }
            catch( Exception ex )
            {
                _logger.LogError( ex.ToString( ) );
            }
        }
    }
}
