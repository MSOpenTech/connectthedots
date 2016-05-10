---
services: event-hubs, iot-hub, cloud-services, notification-hubs
platforms: dotnet
author: spyrossak
---

# Notify users of events received by an event or IoT hub #

If you are using Azure Stream Analytics or Azure Machine Learning to generate alerts based upon data coming from your devices, you have various options on how to display those alerts. For example, as in the [Connect The Dots](https://github.com/Azure/connectthedots) project, you could display them on a website, so users could see them in real-time if they are looking at a web page. By contrast, the code here, in the AppToNotifyUsers solution, provides a very basic and stand-alone application for selected users to be notified of alerts. It does so by creating an Azure Cloud Service (worker role) that monitors the assigned event hub and pushes that data to a notification service specified by the administrator. Notification options in the solution include:

- SMTP
- SMS
- Phone

The user can easily add alternative notification options (such as Twitter), following the work flow in the current solution. Note that each of these solutions require a subscription to an external service (e.g. an email service if notifying users over email). A different architecture, using Twitter for Notifications is shown in Olivier Bloch's posting [Tweet vibration anomalies detected by Azure IoT services on data from an Intel Edison running Node.js](https://azure.microsoft.com/en-us/documentation/samples/iot-hub-nodejs-intel-edison-vibration-anomaly-detection/). 

Note that this solution is a DIY, developer-focused example only. It does not address enterprise requirements such as redundancy, fail-over, restart upon failure, etc. For more comprehensive and production solutions, check out the following:

* Using connectors or push notifications from an Azure Notification Hub available in [Logic Apps](https://azure.microsoft.com/en-us/documentation/articles/app-service-logic-connectors-list) 
* See this [Notification Hubs Overview](https://msdn.microsoft.com/library/azure/jj927170.aspx) for background on Notification Hubs 
* [Broadcast push notifications to millions of mobile devices using Windows Azure Notification Hubs](http://weblogs.asp.net/scottgu/broadcast-push-notifications-to-millions-of-mobile-devices-using-windows-azure-notification-hubs) by Scott Guthrie 


## WARNING ##

This application runs in the cloud, and will push ALL the data your event hub of choice receives to the users you list. The anticipated scenario is that you monitor an event hub that is dedicated to receiving alerts on a sporadic basis (maybe once a day or once a week), in which case your targeted users will get an alert pushed to them once a day or once a week. If, however, you monitor an event hub that is getting data every second then your users will get an alert once a second. Realize that it may take a few minutes to stop a cloud service once it is running, so that your user(s) may get 60 emails a minute until the service is fully shut down if you make the wrong choice - assuming you are at a computer and able to connect to the Azure management portal to stop the service! We strongly suggest you do not set this up and then go away for a two week vacation without testing anticipated scenarios...


# Prerequisites #

In order to configure and deploy the AppToNotifyUsers application you will need to have set up an Event Hub and know the Connection String. The easiest way to do this is to use [AzurePrep](https://github.com/Azure/connectthedots/tree/master/Azure/AzurePrep ) in the Connect The Dots repo, but that is not a prerequisite - set up the Event Hub manually if you like.

You will also need an account or subscription to the user service of your choice - for example an email service such as your ISP, or a computer-to-SMS or Voice service.

# Setup Tasks #

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


# Editing app.config #

There are three sections of App.config you will need to change - to specify the Event Hub to monitor, to specify the method by which the messages will be sent, and to specify the sender and recipients of the messages.

## Step 1: Specifying the Event Hub to Monitor ##
The code in the [AppToNotifyUsers](https://github.com/Azure/connectthedots/tree/master/Azure/AppToNotifyUsers) solution creates an Azure Cloud Service (worker role) that monitors an event hub identified by a URL you list a config file, App.config, together with the Shared Key that grants you access. The strings in App.config that needs to be modified are the following:
```
<add key="Microsoft.ServiceBus.EventHubToMonitor" value="[Event Hub name]" />
<add key="Microsoft.ServiceBus.ServiceBusConnectionString" value="[Service Bus connection string]" />
```
If you deploy the example in the Connect The Dots, that event hub is called ehalerts, and you would replace [Event Hub name] with 'ehalerts', and the ServiceBusConnectionString string with the connection string for it, that you can find in the Azure management portal. It should look something like this:

``` 
<add key="Microsoft.ServiceBus.EventHubToMonitor" value="ehalerts" />
<add key="Microsoft.ServiceBus.ServiceBusConnectionString" value="Endpoint=sb://mynamespace-ns.servicebus.windows.net/;SharedAccessKeyName=StreamingAnalytics;SharedAccessKey=X4a22abcXiRnA3dhBbzu0oHml3a6aLbTNuffrHJ0vHY=" />
```
The EventHubReader module in the code uses this information to get messages from ehalerts, and put it in a queue to be sent by whatever method you specify.

## Step 2: Select the outbound messaging service ##
Notification options in the solution are encoded as separate subroutines that are called depending upon entries in the App.Config file. Currently there are three options included in the sample code:

* SMTP 
* SMS 
* Phone 

As with the Event Hub, you need to specify in App.Config the service you will be using to push the alerts and the credentials for that service. The keys are as follows:
``` 
<add key="NotificationService" value="[Service option]" />
<add key="SmtpHost" value="[host name]" />
<add key="SenderUserName" value="[user name]" />
<add key="SenderPassword" value="[user password]" />
<add key="SmtpEnableSSL" value="true" />
```
If you want to use email to send your alerts, replace [Service option] with 'SMTP', the SMTPHost with your email server name, and enter the credentials that are allowed to use that service. If you want to use SMS, and have a subscription to a service such as Twilio, replace [Service option] with 'SMS', and so forth.  You can add additional or alternative notification options easily, such as using Twitter, following the work flow in the current solution. Note: Each of these solutions require a subscription to an external service (e.g. an email service if notifying users over email).

To repeat what was said earlier, this solution is strictly a DIY, developer-focused example only. For more comprehensive production-level solutions, please check out the other solutions listed at the beginning of this file.
## Step 3: Identify the Sender and Recipients of the Messages ##
Once you have specified how messages will be sent, you need to identify from whom, and to whom, they will be sent. If the Notification Service is SMTP, either using an SMTP host to which you have access, or using SendGrid, you would specify an email address in the sendFrom address in App.Config:
```
  <sendFrom address="sender@outlook.com" displayName="Sender Name" subject="CTD Alerts" />
```	
If you are using an SMTP (email) sender, this would be the display name and email alias shown on the From and Subject lines of the email. Note that the sender address you enter will be that shown on the email as received by the recipient.  If they are expected to reply, that should be a real email recipient. Furthermore, if you are sending a lot of email to various recipients, make sure that they are ok with getting those emails, so your sender does not get labeled as a spammer! 

If you are using a service like Twilio to send SMS messages, this would be the phone number Twilio assigns to you for the sender of the SMS.  Similarly for the recipient list:
```
<sendToList>
<add address="operator1@outlook.com" />
<add address="operator2@outlook.com" />
</sendToList>
```



# Publishing the application #
You use Visual Studio to publish and start the application. The steps are as follows: 
* In Visual Studio, right-click on 'WorkerRole' in Solution 'AppToNotifyUsers', and select *Publish*.
* In the Publish Azure Application, answer the following questions. 
    * Name: [pick something unique]
    * Region: [pick same region as you used for the Event Hub]
    * Database server: no database
    * Database password: [leave suggested password]
* Click Publish, and wait until the status bar shows "Completed". At that point the application is running in your subscription, polling the event hub you listed, and pushing everything it receives to the users you listed over the service you picked.
