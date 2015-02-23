The below instructions will help you setup the Azure services to implement the Temperature and Humidity monitoring scenario. It assumes you have all the necessary software and subscriptions and that you cloned or downloaded the ConnectTheDots.io project on your machine.

##Prerequisites

Make sure you have all software installed and necessary subscriptions:
1. Microsoft Azure subscription ([free trial subscription](http://azure.microsoft.com/en-us/pricing/free-trial/)is sufficient)
1. Access to the [Azure Streaming Analytics Preview](https://account.windowsazure.com/PreviewFeatures)
1. Visual Studio 2013 – [Community Edition](http://www.visualstudio.com/downloads/download-visual-studio-vs)

##Create Azure resources for Event Hub:

* Open the ConnectTheDots\Azure\CloudDeploy\ConnecTheDotsCloudDeploy.sln solution in Visual Studio and build the project from the BUILD menu.
* Download publishsetting file from Azure. This will contain information about your current subscription and be used to configure other components of your solution.
    * Go to https://manage.windowsazure.com/publishsettings/ and save to local disk `<publishsettingsfile>` (contains keys to manage all resources in your subscriptions, so handle with care). Save this to a folder of your choice such as C:\MyTempFolder\MyAzureSubscription.publishsettings
    * **If you have access to multiple subscriptions, make sure the file only contains the subscription that you want to use. Otherwise, edit and remove the other XML elements for the other subscriptions**.
* Run ConnectTheDotsCloudDeploy from an elevated command prompt (“Run as administrator”) , passing a name to be used for all cloud resources, and the publishsetting file (including its full path if not in the same folder as the exe).
    * Chose a `<name>` that has only letters and numbers – no spaces, dashes, underlines, etc and should be **at least 3 characters and less than 47**. (If the publishsettingsfile filename has spaces in it, you will get an error saying the file cannot be found. Surround just the publishsettingsfile with quotation marks and re-run ConnectTheDotsCloudDeploy.exe.)
	
```
cd ConnectTheDots\Azure\CloudDeploy\ConnecTheDotsCloudDeploy\bin\debug\
ConnectTheDotsCloudDeploy.exe –n <name> -ps <publishsettingsfile>
```

* Note the device connection strings displayed by the tool, highlighted below, as you will need them to provision the devices later. You might copy and paste into Notepad for easy retrieval.

```
C:\MyProjectLocation\connectthedots\Azure\CloudDeploy\ConnectTheDotsCloudDeploy\bin\Debug> 
ConnectTheDotsCloudDeploy.exe -n ctdtest1 -ps C:\MyTempFolder\MyAzureSubscription.publishsettings
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

Web.Config saved to C:\MyProjectLocation\connectthedots\Azure\CloudDeploy\ConnectTheDotsWebSite\web.config
```

As you can see above, the ConnectTheDotsCloudDeploy command created two Event Hubs, EHDevices and EHAlerts, also shown in the ConnectTheDots.IO architecture (on the home page of the wiki). It also created four endpoints for AMQP connections from your remote devices such as Raspberry Pi devices. If you did not copy the output above to the a file and closed the window, you can retrieve the endpoint strings by 

1. launching http://manage.windowsazure.com
2. selecting Service Bus in the left nav menu
3. picking your Namespace 
4. select Event Hubs from the top menu
5. select ehdevices
6. select Connection Information tab at the bottom


##Create three Azure Stream Analytics (ASA) jobs

* Make sure you have access to the  ASA preview> If you don’t, sign up at https://account.windowsazure.com/PreviewFeatures 
* Create the first job
    * Open the Azure Management Portal, and create a new job “Aggregates”:
        * "+” in lower left corner -> Data Services -> Stream Analytics -> Quick Create -> Job name “Aggregates”.
    * Create an input
        * Select the Inputs tab in the Aggregates job.
            * Inputs tab -> Add an Input -> Data Stream, Event Hub
        * Input Alias: “DevicesInput”
        * Subscription: “Use Event Hub from Current Subscription”
        * Choose the namespace `<name>`-ns, where `<name>` is the name you created when running the ConnectTheDotsDeploy to create the Event Hubs previously.
        * Event Hub “ehdevices”
        * Policy Name: “StreamingAnalytics”
        * Serialization: JSON, UTF8
    * Create a query 
        * Select the Query tab in the Aggregates job
        * Copy/paste contents “Aggregates.sql” found in the ConnectTheDots\Azure\StreamingAnalyticsQueries folder in Windows Explorer
        * Save
![](https://github.com/MSOpenTech/connectthedots/blob/master/Wiki/Images/AzureStreamAnalyticsQuery.png)
    * Create an output
        * Select the Output tab in the Aggregates job
            * Output tab -> Add an Output, Event Hub,
		* Choose the namespace <name>-ns, 
        * Event Hub “ehalerts”
        * Policy name “StreamingAnalytics”
        * Serialization “JSON”, UTF8
    * Start the Job
        * Dashboard, Start
* Create a second job “Alerts”: as above, but use “alert.sql” contents for the query
* Create a third job “LightSensor”: as above, but use “lightsensor.sql” contents for the query