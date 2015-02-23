The Connect The Dots implementation requires a number of Azure resources and services that need to be created prior to adding devices to the infrastructure, as various configuration parameters for the devices will be depend upon the specific Azure resources created. While these resources can be created manually, in this project we have provided a script that automates this process, described below. It assumes you have all the necessary software and subscriptions and that you cloned or downloaded the ConnectTheDots.io project on your machine.

## Prerequisites ##

Make sure you have all software installed and necessary subscriptions as indicated in the Readme.md file for the project. To repeat them here, you need

1. Microsoft Azure subscription ([free trial subscription](http://azure.microsoft.com/en-us/pricing/free-trial/) is sufficient)
1. Access to the [Azure Streaming Analytics Preview](https://account.windowsazure.com/PreviewFeatures)
1. Visual Studio 2013 – [Community Edition](http://www.visualstudio.com/downloads/download-visual-studio-vs)

## Create Azure resources for IoT infrastructure ##

* Open the ConnectTheDots\Azure\AzurePrep\AzurePrep.sln solution in Visual Studio and build the project from the BUILD menu (Select Release, not Debug).
* Download the publishsettings file from Azure for your subscription. This will contain information about your current subscription and be used to configure other components of your solution. To download this file
    * Go to https://manage.windowsazure.com/publishsettings/ and save to local disk `<publishsettingsfile>` (contains keys to manage all resources in your subscriptions, so handle with care). Save this to a folder of your choice such as C:\MyTempFolder\MyAzureSubscription.publishsettings
    * **If you have access to multiple subscriptions, make sure the file only contains the subscription that you want to use. Otherwise, edit and remove the other XML elements for the other subscriptions**.
* Change directory to the bin\Release directory where the solution built, and run AzurePrep.exe from an elevated command prompt (“Run as administrator”), passing a name to be used for all cloud resources, and the publishsetting file (including its full path if not in the same folder as the exe). Choose a name`that has only letters and numbers – no spaces, dashes, underlines, etc and should be **at least 3 characters and less than 47**. (If the publishsettingsfile filename has spaces in it, you will get an error saying the file cannot be found. Surround just the publishsettingsfile with quotation marks and re-run AzurePrep.exe.):
    
			cd ConnectTheDots\Azure\AzurePrep\AzurePrep\bin\release\
			AzurePrep.exe –n <name> -ps <publishsettingsfile>
			

* Note the device connection strings displayed by the tool, as you will need them to provision the devices later. You might copy and paste into Notepad for easy retrieval.
    
			C:\MyProjectLocation\connectthedots\Azure\AzurePrep\AzurePrep\bin\Release>
			AzurePrep.exe -n ctdtest1 -ps C:\MyTempFolder\MyAzureSubscription.publishsettings
			Creating Service Bus namespace ctdtest1-ns in location Central US
			Namespace cdttest1-ns in state Activating. Waiting...
			Namespace cdttest1-ns in state Activating. Waiting...
			Namespace cdttest1-ns in state Activating. Waiting...
			Creating Event Hub EHDevices
			Creating Consumer Group WebSite on Event Hub EHDevices
			Creating Consumer Group WebSiteLocal on Event Hub EHDevices
			Creating Event Hub EHAlerts
			Creating Consumer Group WebSite on Event Hub EHAlerts
			Creating Consumer Group WebSiteLocal on Event Hub EHAlerts
			Creating Storage Account cdttest1storage in location Central US

			Service Bus management connection string (i.e. for use in Service Bus Explorer):
			Endpoint=sb://ctdtest1-ns.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=zzzzzzz

			Device AMQP address strings (for Raspberry Pi/devices):
			amqps://D1:xxxxxxxx@yyyyyyyy.servicebus.windows.net
			amqps://D2:xxxxxxxx@yyyyyyyy.servicebus.windows.net
			amqps://D3:xxxxxxxx@yyyyyyyy.servicebus.windows.net
			amqps://D4:xxxxxxxx@yyyyyyyy.servicebus.windows.net

			Web.Config saved to C:\MyProjectLocation\connectthedots\Azure\AzurePrep\Website\ConnectTheDotsWebSite\web.config

The AzurePrep.exe command created two Event Hubs, EHDevices and EHAlerts. It also created four endpoints for AMQP connections from your remote devices such as Raspberry Pi devices. If you did not copy the output above to the a file and closed the window, you can retrieve the endpoint strings by 

1. launching http://manage.windowsazure.com
2. selecting Service Bus in the left nav menu
3. picking your Namespace 
4. select Event Hubs from the top menu
5. select ehdevices
6. select Connection Information tab at the bottom


