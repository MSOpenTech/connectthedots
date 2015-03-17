The Connect The Dots implementation requires a number of Azure resources and services that need to be created prior to adding devices to the infrastructure, as various configuration parameters for the devices will be depend upon the specific Azure resources created. While these resources can be created manually, in this project we have provided a script that automates this process, described below. It assumes you have all the necessary software and subscriptions and that you cloned or downloaded the ConnectTheDots.io project on your machine.

## Prerequisites ##

Make sure you have all software installed and necessary subscriptions as indicated in the Readme.md file for the project. To repeat them here, you need

1. Microsoft Azure subscription ([free trial subscription](http://azure.microsoft.com/en-us/pricing/free-trial/) is sufficient)
1. Access to the [Azure Streaming Analytics Preview](https://account.windowsazure.com/PreviewFeatures)
1. Visual Studio 2013 – [Community Edition](http://www.visualstudio.com/downloads/download-visual-studio-vs)

## Create Azure resources for IoT infrastructure ##

###Create Event Hubs###

* Open the ConnectTheDots\Azure\AzurePrep\AzurePrep.sln solution in Visual Studio and build the project from the BUILD menu (Select Release, not Debug).
* Run the application (hitting F5 in Visual Studio or double clicking on the connectthedots\Azure\AzurePrep\AzurePrep\bin\Release\azureprep.exe file)
* Login to Azure and if you have several subscriptions, select the one you want to deploy your services on from the choice provided in the next prompt.
* Enter the namespace prefix you would like for your Service Bus namespace for the Event Hubs
* Choose the location of the datacenter you would like the Service Bus Service to run on
* Note the device connection strings displayed by the tool, as you will need them to provision the devices later. You might copy and paste into Notepad for easy retrieval.
    

			Service Bus management connection string (i.e. for use in Service Bus Explorer):
			Endpoint=sb://ctdtest1-ns.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=zzzzzzz

			Device AMQP address strings (for Raspberry Pi/devices):
			amqps://D1:xxxxxxxx@yyyyyyyy.servicebus.windows.net
			amqps://D2:xxxxxxxx@yyyyyyyy.servicebus.windows.net
			amqps://D3:xxxxxxxx@yyyyyyyy.servicebus.windows.net
			amqps://D4:xxxxxxxx@yyyyyyyy.servicebus.windows.net

The AzurePrep.exe command created two Event Hubs, EHDevices and EHAlerts. It also created four endpoints for AMQP connections from your remote devices such as Raspberry Pi devices. If you did not copy the output above to the a file and closed the window, you can retrieve the endpoint strings by 

1. launching http://manage.windowsazure.com
2. selecting Service Bus in the left nav menu
3. picking your Namespace 
4. select Event Hubs from the top menu
5. select ehdevices
6. select Connection Information tab at the bottom

###Create Website config info###
* Run the application ConnecttheDots\Azure\AzurePrep\CreateWebConfig\bin\Release\CreateWebConfig.exe
*  Login using your Azure subscription credentials
* Enter the namespace prefix and location you chose when creating the Event Hubs
* The application has now created a web.config file and put it in the Website project folder