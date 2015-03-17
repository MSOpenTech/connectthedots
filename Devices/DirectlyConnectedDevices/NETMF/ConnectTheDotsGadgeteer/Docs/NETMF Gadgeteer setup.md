This Wiki explains how to setup a Gadgeteer board running .NET MicroFramework to send temperature and humidity data to Microsoft Azure to do analytics and display real time data as well as alerts.
It assumes that you have all the necessary hardware and tools installed (see below)

##Hardware requirements

The hardware listed below is the one used to test the code. You can try with an other board and adapt the code to the hardware differences (different sensors on different pins, different network connection,...). Important note: you will need a hardware that is SSL capable as the connection with Azure services is established using a secured SSL connection. Not all .NET Micro Framework hardware are SSL capable.

 - [Gadgeteer FEZ Spider Mainboard][1]
 - [Gadgeteer USB DP Module][2]
 - [Gadgeteer Ethernet J11D Module][3]
 - [Gadgeteer Temp&Humidity Module][4]

##Software and tools requirements

You will need to install the below software in this order to implement this .NET Micro Framework sample.

 - Visual Studio 2013 ([Community Edition][5] works fine)
 - [.Net Micro Framework Core SDK][6]
 - [.Net MF Visual Studio 2013 integration][7]
 - [.Net Gadgeteer Core][8]
 - [GHI NETMF and Gadgeteer package][9]
 - [GHI discontinued Gadgeteer Module Drivers][11]
 - [NETMF Toolbox][12]

##VS Solution

* Open the Visual Studio Solution for the .NET Micro Framework sample, located in the repo ConnectTheDots\devices\NETMF\ConnectTheDotsGadgeteer.sln.

* Open the program.cs file and edit the 6 lines below to configure the AMQP connection to Event Hub using the information you got when setting up the Azure services using the ConnectTheDotsCloudDeploy tool. For the device name, guid, organization and location, pick one of your choosing.

```
const string AMQPAddress = "amqps://{key-name}:{shared-key}@{namespace}.servicebus.windows.net";
const string EventHub = "{EventHub-name}";
const string SensorName = "{sensor-name}";
const string SensorGUID = "{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}";
const string Organization = "{organization}";
const string Location = "{location}";
```

* Once you have applied these changes to the code, connect the temperature & humidity sensor, the Ethernet adapter and the USB power extension to the Gadgeteer FEZ mainboard as shown in the picture below (temp sensor connected to slot 4, Ethernet adapter to slot 7, USB DP module to slot 1).

* Connect the Ethernet Module to an Ethernet cable connected to a network with DHCP, connect the USB cable from your PC to the USB DP Module.

* When hitting F5 in Visual Studio, the Gadgeteer board will be flashed with your program and will start sending its temperature and humidity data to Azure services.

If you are having issues with the Gadgeteer board, check out the [troubleshooting guide][10] from GHI Electronics. 

If you are hitting an exception when running the application on your board in the InitAMQPConnection function when the new Connection call is made (see below), this probably means you need to update the SSL seed on your board, with the MFDeploy.exe tool using the Target|ManageDeviceKeys|Update SSL Seed menu.

```csharp
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
```

  [1]: https://www.ghielectronics.com/catalog/product/269
  [2]: https://www.ghielectronics.com/catalog/product/280
  [3]: https://www.ghielectronics.com/catalog/product/284
  [4]: https://www.ghielectronics.com/catalog/product/344
  [5]: http://go.microsoft.com/?linkid=9863608
  [6]: http://netmf.codeplex.com/downloads/get/911182
  [7]: http://netmf.codeplex.com/downloads/get/911183
  [8]: http://gadgeteer.codeplex.com/downloads/get/918081
  [9]: https://www.ghielectronics.com/support/netmf/sdk/24/netmf-and-gadgeteer-package-2014-r5
  [10]: https://www.ghielectronics.com/docs/165/netmf-and-gadgeteer-troubleshooting
  [11]: https://www.ghielectronics.com/docs/299/discontinued-gadgeteer-module-drivers
  [12]: http://netmftoolbox.codeplex.com/