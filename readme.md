[ConnectTheDots.io](http://connectthedots.io) is an open source project created by <a href="http://msopentech.com">Microsoft Open Technologies</a> to help you get tiny devices connected to Microsoft Azure, and to implement great IoT solutions taking advantage of Microsoft Azure advanced analytic services such as Azure Stream Analytics and Azure Machine Learning. 

In this project there are code samples, configuration scripts and guides that will help you set up devices and sensors, and configure Microsoft Azure services to view and analyze the data produced by those devices. Some of these samples have been provided by MS Open Tech, others by third parties; we encourage everyone to submit code samples or configuration documentation to grow this project.

A good first task, which we are calling the "Connect The Dots starter solution" is to build a simple temperature sensing network. It can be built quickly and easily with minimal knowledge of programming or Microsoft Azure, using commodity devices available locally or online - for example an Arduino UNO board with a weather shield, connected to a Raspberry Pi sending data to an Azure website. 


![](Arduino-Pi-IoT.png)


Sample code for this is included in the project, as well as for many other more elaborate scenarios.

## Device basics ##
The current project is built on the premise that data from sensors is sent to an Azure Event Hub in a prescribed JSON format. The minimum structure, with required attribute names, is 

    {
	"guid" 			:	"string",
	"organization"	:	"string",
	"displayname"	:	"string",
	"location"		:	"string",
	"measurename"	:	"string",
	"unitofmeasure"	:	"string",
	"value" 		:	double/float/integer
	}
	
This should all be sent as one string message to the Event Hub, for example as the following strings: 

    {"guid":"62X74059-A444-4797-8A7E-526C3EF9D64B","organization":"My Org Name","displayname":"Sensor Name","location":"Sensor Location","measurename":"Temperature","unitofmeasure":"F","value":74}

or

    {"guid":"62X74059-A444-4797-8A7E-526C3EF9D64B","organization":"my org name","displayname":"sensor name","location":"sensor location","measurename":"Temperature","unitofmeasure":"F","value":74.0001}


Furthermore, the project is built upon the premise that the *sensors* create and format this JSON string. For example, if using a sensor attached to an Arduino, the code running on the Arduino would send successive JSON strings, CRLF ended, out the serial port to a gateway such as a Raspberry Pi or Windows Tablet. The gateway does nothing other than receive the JSON string, package that into an AMQP message, and send it to Azure. In the case of a directly connected device, the latest needs to send the JSON package to the event hub whether encapsulating the JSON message in an AMQP message or sending the JSON message in a REST packet.

All the device code included in this project, or submitted for inclusion, must conform to the JSON format requirement above. 

## Software prerequisites ##
In order to reproduce one of the ConnectTheDots.io scenarios, you will need the following:

1. Microsoft Azure subscription ([free trial subscription](http://azure.microsoft.com/en-us/pricing/free-trial/) is sufficient)
2. Access to the [Azure Streaming Analytics Preview](https://account.windowsazure.com/PreviewFeatures)
3. Visual Studio 2013 – [Community Edition](http://www.visualstudio.com/downloads/download-visual-studio-vs) or above

## Hardware prerequisites for Connect The Dots starter solution ##
If you are going to deploy the starter solution, you need to procure an Arduino UNO and Raspberry Pi, as shown in the documentation for those devices in the appropriate folders:

- [Arduino UNO R3 and weather shield](Devices/GatewayConnectedDevices/Arduino/Weather/WeatherShieldJson/Hardware.md)
- [Raspberry Pi](Devices/Gateways/RaspberryPi/Hardware.md)

If you decide to connect another device, you can check out the samples provided in the devices sub folder containing .NET and C++ examples. Other languages examples are coming soon!

## Setup Tasks ##
Setting up your IoT solution involves several distinct steps, each of which is fully described in this project:


1. [Azure prep](Azure/AzurePrep/AzurePrep.md) - Creating basic Azure resources
2. [Device setup](Devices/DeviceSetup.md) - Configuring your device(s)
3. [Sample website deployment](Azure/WebSite/WebsitePublish.md) - Publishing a generic sample website for viewing the data
4. [Analysis services setup](Azure/StreamAnalyticsQueries/SA_setup.md) - Configuring Azure Stream Analytics services (for starter solution)
  
To get started with a simple example, complete the "Connect The Dots starter solution" tasks identified in each of the above steps.

## Run the scenario ##

Once you have setup the services, published the site, provisioned and connected your devices, you will see data coming up on your website at the URL you chose when deploying the site.

You should see average temperature measurements showing up in the web browser every 20 seconds.

If you select “All”, you should see raw readings from the device coming in every second.
If the temperature exceeds 75 degrees (F), you should see an alert showing in the alerts table, once every 20 seconds while the temperature on any of the devices exceeds 75 degrees (F).
If you cover the shield, you will see an alert telling you the light is turned off.

![](https://github.com/MSOpenTech/connectthedots/blob/master/Wiki/Images/WebSiteCapture.png)
