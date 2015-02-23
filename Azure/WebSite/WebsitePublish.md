This document explains how to build and deploy a sample website that is used to show data and alerts in the Connect The Dots project. It assumes you have all necessary software and subscriptions and that you have cloned or download the ConnectTheDots.io project on your machine.

## Prerequisites ##

Make sure you have all software installed and necessary subscriptions as indicated in the Readme.md file for the project. To repeat them here, you need

1. Microsoft Azure subscription ([free trial subscription](http://azure.microsoft.com/en-us/pricing/free-trial/) is sufficient)
1. Access to the [Azure Streaming Analytics Preview](https://account.windowsazure.com/PreviewFeatures)
1. Visual Studio 2013 – [Community Edition](http://www.visualstudio.com/downloads/download-visual-studio-vs)


## Publish the Azure Website ##

* Open the ConnectTheDots\Azure\WebSite\ConnectTheDotsWebSite.sln solution in Visual Studio
* In VS, Right-click on the project name and select Publish.
* Select Azure Web Sites, create new one. 
    * Site name: [pick something unique]
    * Region: [pick same region as you used for Stream Analytics]
    * Database server: no database
    * Password: [leave suggested password]
* Publish (you might need to install WebDeploy extension if you are having an error stating that the Web deployment task failed. You can find WebDeploy [here](http://www.iis.net/downloads/microsoft/web-deploy)).

## Websockets setting ##
* Enable WebSockets for the new Azure Web site
    * Browse to https://manage.windowsazure.com and select your Azure Web Site.
    * Click on the Configure tag. Then set WebSockets to On and Click "Save"
	
##Running the site
* Open the site in a browser to verify it has deployed correctly. 
    * At the bottom of the page you should see “Connected.”. If you see “ERROR undefined” you likely didn’t enable WebSockets for the Azure Web Site (see step d above).
