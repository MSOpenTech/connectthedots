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

using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using GHI.Networking;
using Microsoft.SPOT.Net.NetworkInformation;

using Toolbox.NETMF;
using Toolbox.NETMF.NET;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

using Amqp;
using System.Text;
using Amqp.Framing;
using Amqp.Types;

using Json.NETMF;

namespace ConnectTheDotsGadgeteer
{
    public partial class Program
    {
        // Azure Event Hub connection information
        const string AMQPAddress = "amqps://{key-name}:{key}@{namespace-name}.servicebus.windows.net"; // Azure event hub connection string
        const string EventHub = "{eventhub-name}"; // Azure event hub name
        const string SensorName = "Gadgeteer"; // Name of the device you want to display
        const string SensorGUID = "{GUID}"; // unique GUID per device. Use GUIDGEN to generate new one
        const string Organization = "MSOpenTech"; // Your organization name
        const string Location = "My Room"; // Location of the device

        // Define the frequency at which we want the device to send its sensor data (in milliseconds)
        const int SendFrequency = 1000;

        // Initialization thread
        private Thread initThread;

        // AMQP connection to Azure Event Hubs resources
        private Address address = null;
        private Connection connection = null;
        private Session session = null;
        private SenderLink sender = null;
        // Using InterlockExchange to ensure there is no concurency in using the AMQP connexion
        private static int IsSendingMessage = 0;

        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");

            initThread = new Thread(new ThreadStart(this.Init));
            initThread.Start();
        }

        // Initialization function
        private void Init()
        {
            // Initialize network
            //            initWiFi("MSOT-IOT", "MSOpenTechIoT9!");
            InitEthernet();

            // get Internet Time using SNTP
            GetInternetTime();

            // Intiializa AMQP connection with Azure Event Hub
            InitAMQPconnection();

            //Initialize temperature and humidtiy sensor and start capture loop
            tempHumidity.MeasurementInterval = SendFrequency;
            tempHumidity.MeasurementComplete += new TempHumidity.MeasurementCompleteEventHandler(tempHumidity_MeasurementComplete);
            tempHumidity.StartTakingMeasurements();
        }

        // Initialization of AMQP connection to Azure Event Hubs
        private void InitAMQPconnection()
        {
            // Get the Event Hub URI
            address = new Address(AMQPAddress);

            // create connection
            connection = new Connection(address);

            // create session
            session = new Session(connection);

            // create sender
            sender = new SenderLink(session, "send-link", EventHub);
        }

        // Initialization of Ethernet connection using DHCP (default)
        void InitEthernet()
        {
            // Open the Interface
            while (!ethernetJ11D.NetworkInterface.Opened)
                ethernetJ11D.NetworkInterface.Open();

            // Wait for DHCP address
            while (ethernetJ11D.NetworkInterface.IPAddress == "0.0.0.0")
            {
                Debug.Print("Waiting for network ...");
                Thread.Sleep(1000);
            }

            // Debug print info about IP adress
            Debug.Print("Network is ready!");
            Debug.Print("IP Address: " + ethernetJ11D.NetworkInterface.IPAddress);
        }

        // Retreive Internet Time using SNTP library from NETMF toolbox
        // The board has no battery and we need to timestamp the data sent to Azure
        void GetInternetTime()
        {
            // Initializes the time client
            SNTP_Client TimeClient = new SNTP_Client(new IntegratedSocket("time-a.nist.gov", 123));

            // Displays the time in three ways:
            Debug.Print("Amount of seconds since 1 jan. 1900: " + TimeClient.Timestamp.ToString());
            Debug.Print("UTC time: " + TimeClient.UTCDate.ToString());
            Debug.Print("Local time: " + TimeClient.LocalDate.ToString());

            // Synchronizes the internal clock
            TimeClient.Synchronize();
        }

        //void initWiFi(String ssid, String passphrase)
        //{
        //    bool foundSSID = false;
        //    if (!wifiRS21.NetworkInterface.Opened)
        //        wifiRS21.NetworkInterface.Open();

        //    wifiRS21.NetworkInterface.EnableDhcp();
        //    wifiRS21.UseDHCP();
        //    //wifi.UseStaticIP("192.168.1.225", "255.255.255.0", "192.168.1.1", new string[] { "10.1.10.1" });

        //    wifiRS21.NetworkDown += new GTM.Module.NetworkModule.NetworkEventHandler(wifiRS21_NetworkDown);
        //    wifiRS21.NetworkUp += new GTM.Module.NetworkModule.NetworkEventHandler(wifiRS21_NetworkUp);

        //    Debug.Print("Scan for wireless networks");
        //    WiFiRS9110.NetworkParameters[] scanResult = wifiRS21.NetworkInterface.Scan();
        //    if (scanResult != null)
        //    {
        //        foreach (WiFiRS9110.NetworkParameters x in scanResult)
        //        {
        //            if (x.Ssid == ssid) foundSSID = true;
        //            Debug.Print(x.Ssid.ToString());
        //        }
        //    }
        //    else
        //    {
        //        Debug.Print("No wireless networks were found.");
        //    }
        //    Debug.Print("------------------------------------");
        //    if (foundSSID)
        //    {
        //        Debug.Print("Connecting to " + ssid);
        //        wifiRS21.NetworkInterface.Join(ssid, passphrase); // Network with WPA or WPA2 security.
        //        Thread.Sleep(1000);
        //        Debug.Print("Connected");
        //        Debug.Print("IP Address: " + wifiRS21.NetworkSettings.IPAddress);
        //    }
        //    else
        //    {
        //        Debug.Print(ssid + " Wireless network was not found");

        //    }
        //    Debug.Print("------------------------------------");
        //}

        //void wifiRS21_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        //{
        //    Debug.Print("Network down");
        //}

        //void wifiRS21_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        //{
        //    Debug.Print("Network Up");
        //    Debug.Print("IP Address: " + wifiRS21.NetworkSettings.IPAddress);
        //}

        // Callback function for measurement of temp and humidity. called every SendFrequency milliseconds
        void tempHumidity_MeasurementComplete(TempHumidity sender, TempHumidity.MeasurementCompleteEventArgs e)
        {
            // Convert temperature into Fahrenheit
            var FahrenheitTemp = e.Temperature * 9 / 5 + 32;

            // Display data and time in debug console
            Debug.Print(DateTime.Now.ToString());
            Debug.Print("Temperature=" + FahrenheitTemp.ToString());
            Debug.Print("Humidity=" + e.RelativeHumidity.ToString());

            // send to Event Hub
            SendAMQPMessage(FormatMessage("Temperature", "F", FahrenheitTemp));
            SendAMQPMessage(FormatMessage("Humidity", "%", e.RelativeHumidity));
        }

        string FormatMessage(string measureName, string unitOfMeasure, double value)
        {
            // Create hashtable for data
            Hashtable hashtable = new Hashtable();
            hashtable.Add("organization", Organization);
            hashtable.Add("location", Location);
            hashtable.Add("guid", SensorGUID);
            hashtable.Add("displayname", SensorName);
            hashtable.Add("unitofmeasure", unitOfMeasure);
            hashtable.Add("measurename", measureName);
            hashtable.Add("value", value);
            hashtable.Add("timecreated", DateTime.UtcNow);

            // Serialize hashtable into JSON
            JsonSerializer serializer = new JsonSerializer(DateTimeFormat.Default);
            string payload = serializer.Serialize(hashtable);

            return payload;
        }

        // Send a message to Azure Event Hubs using AMQP protocol
        // we are using the sender resource created in the intialization
        void SendAMQPMessage(string payload)
        {
            try
            {
                // 0 indicates the method is not in use
                if (0 == Interlocked.Exchange(ref IsSendingMessage, 1))
                {
                    // Create the AMQP message
                    var encodedTextData = Encoding.UTF8.GetBytes(payload);
                    var message = new Message() {
                        BodySection = new Data ()
                        {
                            Binary = encodedTextData
                        },
                        Properties = new Properties()
                    {
                        CreationTime = DateTime.UtcNow,
                        ContentType = "text/json",
                        }
                    };

                    message.MessageAnnotations = new MessageAnnotations();
                    message.MessageAnnotations[new Symbol("x-opt-partition-key")] = SensorGUID;
                    message.ApplicationProperties = new ApplicationProperties();

                    sender.Send(message);

                    // release lock
                    Interlocked.Exchange(ref IsSendingMessage, 0);
                }
            }
            catch (Exception e)
            {
                Debug.Print("Exception caught:" + e.Message);
            }
        }
    }
}
