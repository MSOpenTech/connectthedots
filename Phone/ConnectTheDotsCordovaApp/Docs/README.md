# ConnectTheDotsCordovaApp

Cross-platform mobile application for controlling things over the cloud.

## Requirements:
- Apache Cordova 4.2.0 (installation guide http://cordova.apache.org/docs/en/4.0.0//guide_cli_index.md.html#The%20Command-Line%20Interface)
- jQuery 1.11.2 (http://jquery.com/)
- jQuery Mobile 1.4.5 (http://jquerymobile.com/)

## Step 0. WebSite (Azure\Website) with REST API should be deployed 
URL to service should be edited in connectthedots\Phone\ConnectTheDotsCordovaApp\www\js\index.js
```javascript
var serviceUri = "http://{website}.azurewebsites.net";
```

## Step 1. Adding platforms 
```shell
connectthedots\Phone\ConnectTheDotsCordovaApp> cordova platform add wp8
connectthedots\Phone\ConnectTheDotsCordovaApp> cordova platform add android
connectthedots\Phone\ConnectTheDotsCordovaApp> cordova platform add ios
...
```

## Step 2. Deploy to Emulator 
```shell
connectthedots\Phone\ConnectTheDotsCordovaApp> cordova emulate wp8
connectthedots\Phone\ConnectTheDotsCordovaApp> cordova emulate android
connectthedots\Phone\ConnectTheDotsCordovaApp> cordova emulate ios
...
```

First time run will be long as image with OS initialising. 

## Step 3. Deploy to Device
```shell
connectthedots\Phone\ConnectTheDotsCordovaApp> cordova run wp8
connectthedots\Phone\ConnectTheDotsCordovaApp> cordova run android
connectthedots\Phone\ConnectTheDotsCordovaApp> cordova run ios
...
```




