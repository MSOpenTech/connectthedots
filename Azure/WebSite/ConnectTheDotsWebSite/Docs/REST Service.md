# REST Service

Solution provide REST API that's controlling devices. Look to Intel Galileo Service software example to develop the on-board part of a system.

[deviceId] - name of a subscription of a topic / or "all" if command should be sent to all the devices.

- `POST http://{website}.azurewebsites.net/api/DevicesControlService/ChangeStateOfLight?deviceId={deviceId}`
		[body] state=off // Turn off the LED
		[body] state=on // Turn on the LED
		
- `POST http://{website}.azurewebsites.net/api/DevicesControlService/WriteTextOnScreen?deviceId={deviceId}`
		[body] text={your text}&br={red}&bg={green}&bb={blue} // print {your text} on screen with RGB highlights
		
- `POST http://{website}.azurewebsites.net/api/DevicesControlService/Beep?deviceId={deviceId}`
		[body] freq={frequency}&delay={duration}
		choose your frequency here https://github.com/Seeed-Studio/Sketchbook_Starter_Kit_V2.0/blob/master/Grove_Buzzer/Grove_Buzzer.ino
		
