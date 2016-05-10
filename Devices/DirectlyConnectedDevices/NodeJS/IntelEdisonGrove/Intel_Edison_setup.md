This document explains how to set up an Intel Edison board to send data to AzureIoT services using the REST interface. 
It assumes that you have the right tools installed and that you have cloned or downloaded the ConnectTheDots.io project on your machine.

##Hardware requirements ##
Check out the hardware requirements [here](hardware.md).
Connect the sensors as follows on the Grove Base shield (itself plugged onto the Intel Edison Arduino brakout board). Note that you can change the connections, but will have to adapt the code if you do so.

Grove Light Sensor           -   A0
Grove Moisture Sensor        -   A1
Grove Rotary potentiometer   -   A2
Grove UV Sensor              -   A3
Grove LCD RGB Backlight      -  I2C
Grove Temperature&Humidity   -  I2C
Grove PIR motion Sensor      -   D7
Grove Buzzer                 -   D4
Grove Button                 -   D6

Note that all sensors are not required to run the sample. Just edit inteledisonctd.js to comment the lines related to the sensor you don't have.

##Prerequisites ##

To deploy the application you will need the following:

* For Windows, download PuTTY and PSCP from [here](http://www.putty.org/).
* WiFi Internet access for the device.

To work on the code of the project, you can use your favorite editor.

## Configure the Edison##

* Follow the instructions on the [Intel support page](https://communities.intel.com/docs/DOC-23192) to setup the Yocto image on the Edison board.
* Connect to the Intel Edison from your laptop, via a USB using PuTTY (or your favorite remote terminal tool)

* In the remote terminal, you can setup the WiFi connection typing:
                
                 configure_edison --wifi

* Once WiFi is setup you can connect your SSH tool through the network using the IP address displayed when doing the setup at previous step or using the command ifconfig.
* the VI text editor comes by default on the Yocto image for the Intel Edison, but if you prefer Nano, connect with SSH and type the following commands:

                 opkg install http://repo.opkg.net/edison/repo/core2-32/ncurses-terminfo_5.9-r15.1_core2-32.ipk
                 opkg install http://repo.opkg.net/edison/repo/core2-32/nano_2.2.5-r3.0_core2-32.ipk


##Prepare settings files##
You will need to edit the settings file of the application before deploying the application on the board to apply your own connection information for your Azure Event Hub.

* Open the file connectthedots\Devices\DirectlyConnectedDevices\NodeJS\IntelEdisonXadow\settings.json and edit the settings based on the configuration of your ehdevices Event Hub. Create a guid (i.e. using guidgen.com) for the device.

                 "namespace": "{namespace}",
                 "keyname": "{key-name}",
                 "key": "{key}",
                 "eventhubname": "ehdevices",
                 "guid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
                 "displayname": "Edison",
                 "organization": "My Org",
                 "location":  "My location"

##Deploy the app##

The deployment is done using the deploy.cmd file found in the scripts subfolder:

* Edit the deploy.cmd file and enter your Edison's IP address as well as the location on your machine of the PuTTY tools.
* Start the deploy.cmd script in a cmd shell. This will copy the app files into the /node_app_slot folder on the device and will update the node packages required for the app to run.
* Once the files are copied in the /node_app_slot, the app will start automatically at reboot. You can simply reboot the board to start sending data Azure!


