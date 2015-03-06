# Intel Galileo Service

Application that provide service functionality on Intel Galileo board for Groove Starter Kit Plus components: 
 - LCD RGB screen : write text and change highlight color (connected to I2C bus)
 - Buzzer : beep function (connected to D4)
 - LED (can be replaced by a relay) : turn off and turn on (connected to D3) 

Commands are coming from Azure Service Bus Topic/Subscriptions over AMQP 1.0.
 
## Libraries
 - SimpleJSON Library by Mike Anchor
 - rgb_lcd by Seeed Technology Inc
 - RapidXml by Marcin Kalicinski
 - unofficial Apache.Qpid.Proton.Unoff.IA32 nuget package with Qpid Proton C and OpenSSL
 
## Installation
 - Connect components to your Intel Galileo Board
 - Configure App.config of CloudDeploy project (Microsoft.ServiceBus.ConnectionString)
 - Build and run CloudDeploy and you'll get connection information for Topic/Subscriptions
 - Configure your devices (IntelGalileoService.exe.config in IntelGalileoService project) with obtained information
 - Configure you remote debugger and run solution
 
## Messages & Json Fields:
 
 Maximum of message size is BUFFER_SIZE (2048 by default) in IntelGalileoService\amqp\Receiver.h
 
 Print message on the screen:
 
```javascript
 {
	"command": "writetextonscreen",
	"text": "[text]",
	"rgb": "[red],[green],[blue]" // 0 <= [red],[green],[blue] <= 255
 }
```
 
 Making beep with setted duration and frequency:
 
```javascript
 {
	"command": "beep",
	"delay": [duration/long],
	"freq": [frequency/long]
 }
```
 
 Switch state of LED: 
 
```javascript
 {
	"command": "beep",
	"state": "[on|off]"
 }
```
 