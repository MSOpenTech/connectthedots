This document explains how to set up the Windows Phone application to send the Microsoft Band sensors data to Azure Event Hub using the REST interface. 
It assumes that you have the right tools installed and that you have cloned or downloaded the ConnectTheDots.io project on your machine.

##Hardware requirements ##
Check out the hardware requirements [here](Hardware.md).

##Prerequisites ##

To deploy the application you will need the following:

* Visual Studio 2013 [Community Edition](http://www.visualstudio.com/downloads/download-visual-studio-vs) or above.
* [Windows Phone 8.1 SDK ](http://dev.windows.com/en-us/develop/download-phone-sdk) (is an installation option of Visual Studio 2013)

##Deploy the app##

There is no specific configuration needed for the application to run. You simply need to follow the following steps:

* Open the solution connectthedots\Devices\DirectlyConnectedDevices\WindowsPhone\ConnectTheDotsWPBand.sln in Visual Studio.
* Build the solution.
* Ensure your Windows Phone is developer unlocked and deploy the application on the device. (You can find great resources to learn about Windows Phone development [here](http://dev.windows.com/en-us) if you need)
* Ensure your Microsoft Band is paired with your phone and no other Band app is running on your Windows Phone.
* Run the application on the phone. At first start, it will bring up the configuration panel
* Fill in the fields using the Event Hub connection settings collected during the Azure Prep step. Here are some things to pay attention to:
    * The Service Bus Namespace is in the format "mynamespace-ns" if you used the AzurePrep tool
    * The Event Hub name is ehdevices if you used the AzurePrep tool
    * the Access Key should NOT be URL encoded. Just use the key as displayed in the Azure portal site
    * For the displayname, chose a short name (less than 16 characters) with no spaces or special characters
* Once you have setup the connection you can press on the Ok button and you will be redirected to the application main screen
* On the app main screen you can select which sensors you want to send the data for to Azure by checking the check boxes of your choice.

If you are having issues connecting with the band, restart the application.
The application will send data only if it's opened (no data is sent when the app is in the background).



