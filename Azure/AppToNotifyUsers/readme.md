## Application to Notify Users of Alerts in Event Hub ##

If you are using Azure Stream Analytics or Azure Machine Learning to generate alerts based upon data coming from your devices, you have various options on how to display those alerts. In the examples in the [Getting Started](https://github.com/Azure/connectthedots/blob/master/GettingStarted.md ) section of [Connect The Dots ](https://github.com/Azure/connectthedots ) there are code samples and configuration scripts that show how to display these alerts on the sample ASP.NET website by first sending them to a separate event hub called ehalerts. 

The code in the [AppToNotifyUsers](https://tbd) solution extends the basic solution in Connect The Dots so that selected users can be notified of data surfaced in the event hub of their choice (alerts pushed to ehalerts in the Getting Started scenario). It does so by creating an Azure Cloud Service (worker role) that monitors the assigned event hub and pushes that data to a notification service specified by the administrator. Notification options in the solution include:

- SMTP
- SMS
- Phone

The user can add additional or alternative notification options easily, following the work flow in the current solution. A different architecture, using Twitter for Notifications is shown in Olivier Bloch's posting [Tweet vibration anomalies detected by Azure IoT services on data from an Intel Edison running Node.js](https://azure.microsoft.com/en-us/documentation/samples/iot-hub-nodejs-intel-edison-vibration-anomaly-detection/). Note that each of these solutions require a subscription to an external service (e.g. an email service if notifying users over email). 

Note that this solution is a DIY, developer-focused example only. It does not address enterprise requirements such as redundancy, fail-over, restart upon failure, etc. For more comprehensive and production solutions, the user should review options such as using connectors available in [Logic Apps](https://azure.microsoft.com/en-us/documentation/articles/app-service-logic-connectors-list/), or push notifications from an Azure Notification Hub. For background on Notification Hubs, see this [Notification Hubs Overview](https://msdn.microsoft.com/library/azure/jj927170.aspx) and Scott Guthrie’s blog [Broadcast push notifications to millions of mobile devices using Windows Azure Notification Hubs](http://weblogs.asp.net/scottgu/broadcast-push-notifications-to-millions-of-mobile-devices-using-windows-azure-notification-hubs).  

## WARNING ##

This application runs in the cloud, and will push ALL the data your event hub of choice receives to the users you list. The anticipated scenario is that you monitor an event hub that is dedicated to receiving alerts on a sporadic basis (maybe once a day or once a week), in which case your targeted users will get an alert pushed to them once a day or once a week. If, however, you monitor an event hub that is getting data every second then your users will get an alert once a second. Realize that it may take a few minutes to stop a cloud service once it is running, so that your user(s) may get 60 emails a minute until the service is fully shut down if you make the wrong choice - assuming you are at a computer and able to connect to the Azure management portal to stop the service! We strongly suggest you do not set this up and then go away for a two week vacation without testing anticipated scenarios...


## Prerequisites ##

In order to configure and deploy the AppToNotifyUsers application you will need to have set up an Event Hub and know the Connection String. The easiest way to do this is to use [AzurePrep](https://github.com/Azure/connectthedots/tree/master/Azure/AzurePrep ), but that is not a prerequisite - set up the Event Hub manually if you like.

You will also need an account or subscription to the user service of your choice - for example an email service such as your ISP, or a computer-to-SMS or Voice service.

## Setup Tasks ##

Setting up the application once you have an Event Hub and its Connection String involves the following tasks, which will be described in greater detail below.

1. Get and set up a subscription for the notification service of your choice
2. Clone or copy the project to your machine 
2. Open the `ConnectTheDots\Azure\AppToNotifyUsers\AppToNotifyUsers.sln` solution in Visual Studio
3. Edit App.config in the Worker Host folder to provide the following
	1. The connection string to your Event Hub
	2. The URL for the web service which you will use to push the data
	3. The credentials you will use for accessing that web service
	4. The information about the sender
	5. The information about the users to be notified
4. Build the project from the *BUILD* menu (Select Release, not Debug)
5. Publish the application to your Azure subscription
6. Generate an alert by doing something to one of your devices, and verify that the alert is making it to your event hub (ehalerts)
7. Verify that the alert is making it to the targeted user (email, SMS, or phone call received by the listed users).


## Editing app.config ##

There are three sections of App.config you will need to change

**The sender name that will be displayed to the recipient of the alerts**. Find the section

	<sendFrom
	    address="sender@outlook.com"
	    displayName="Sender Name"
	    subject="CTD Alerts" />

Replace the fields with information that identifies the sender. If you are using an SMTP (email) sender, this would be the display name and email alias shown on the From: and Subject: lines of the email. If you are using a service like Twilio to send SMS messages, this would be the phone number Twilio assigns to you for the sender of the SMS. 

**The recipient information**. Find the section

	  <sendToList>
	    <add address="operator1@outlook.com" />
	    <add address="operator2@outlook.com" />
	  </sendToList>

Replace the fields with information that identifies one or more recipients of the alerts. If you are using SMTP, this would be a list of email addresses. If you are using SMS or phone calling, this would be a list of phone numbers.

**The connection string to your Event Hub**. Find the strings

	    <add key="Microsoft.ServiceBus.EventHubToMonitor" value="[event hub name]" /> 
	    <add key="Microsoft.ServiceBus.EventHubConnectionString" value="[event hub connection string]" />

Replace the [event hub name] with the name for your event hub, and the EventHubConnectionString string with the connection string for your Event Hub. It should look something like this:

	    <add key="Microsoft.ServiceBus.EventHubToMonitor" value="ehalerts" /> 
	    <add key="Microsoft.ServiceBus.EventHubConnectionString" value="Endpoint=sb://mynamespac-ns.servicebus.windows.net/;SharedAccessKeyName=StreamingAnalytics;SharedAccessKey=X4a22abcXiRnA3dhBbzu0oHml3a6aLbTNuffrHJ0vHY=" />

**The service you will be using to push the alerts and the credentials for that service**. Just below the connection string section you will find a set of assignments for those values.

	    <add key="NotificationService" value="Smtp" />
	    <add key="SmtpHost" value="[host name]" />
	    <add key="SenderUserName" value="[user name]" />
	    <add key="SenderPassword" value="[user password]" />    
	    <add key="SmtpEnableSSL" value="true" />

Enter the information appropriate to the service that you will be using.

## Publishing the application ##

* In Visual Studio, right-click on 'WorkerRole' in Solution 'AppToNotifyUsers', and select *Publish*.
* In the Publish Azure Application, answer the following questions. 
    * Name: [pick something unique]
    * Region: [pick same region as you used for the Event Hub]
    * Database server: no database
    * Database password: [leave suggested password]
* Click Publish, and wait until the status bar shows "Completed". At that point the application is running in your subscription, polling the event hub you listed, and pushing everything it receives to the users you listed over the service you picked.
