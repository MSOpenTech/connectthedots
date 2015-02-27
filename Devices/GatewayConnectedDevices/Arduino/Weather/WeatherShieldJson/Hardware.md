To set up an Arduino UNO with a weather shield to monitor temperature, humidity, and a host of other things you will need to procure:

1. [Arduino Uno R3](http://arduino.cc/en/Main/ArduinoBoardUno)
1. [Sparkfun Weather Shield](https://www.sparkfun.com/products/12081) for Arduino 
1. 4 stackable headers for the Arduino UNO R3 (1 @ 10 pin, 2 @ 8 pin, 1 @ 6 pin), as specified on the Arduino or Sparkfun sites. 
1. USB A to B cable to connect the Arduino to the Raspberry Pi. Make sure it is USB 2.0 not 3.0, as the 3.0 connector does not fit the Arduino.

You will need to solder the headers to the Weather Shield (or have someone do that for you - 10 minute job, max.)

Note: Only the models above have been tested. The Weather Shield sample code from Sparkfun, for example, may need to be modified before it will work reliably on Arduino Due or Arduino Uno R2.