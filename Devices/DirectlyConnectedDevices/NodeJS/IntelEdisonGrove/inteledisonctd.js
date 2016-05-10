//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

// We are using the johnny-five library to access the hardware resources,'
var five = require('johnny-five');
var Edison = require("edison-io");
var TH02 = require("th02js");
var connectthedots = require('./connectthedots.js');
var devicesettings = require('./settings.json');

// [Linear Interpolation](https://en.wikipedia.org/wiki/Linear_interpolation)
function linear(start, end, step, steps) {
  return (end - start) * step / steps + start;
}

// ---------------------------------------------------------------
// Let's connect_the_dots
// You can adapt the below code to your own sensors configuration
function connect_the_dots()
{
    console.log("Device Ready to connect its dots");
    
    // Initialize LCD
    var lcd = new five.LCD({
        controller: "JHD1313M1"
    });
    lcd.bgColor(0xFF, 0xFF, 0xFF).cursor(0,0).print('ConnectTheDots');
    lcd.bgColor(0xFF, 0xFF, 0xFF).cursor(1,0).print('Azure IoT');

    // Initialize temperature sensor
    var data_temperature = {"measurename":"Temperature", "unitofmeasure":"C", "value":0};
    var data_humidity = {"measurename":"Humidity", "unitofmeasure":"%", "value":0};   
    var temperature_humidity = new TH02(6);
    
    setInterval(function(){
        data_temperature.value = temperature_humidity.getCelsiusTemp();
        data_humidity.value = temperature_humidity.getHumidity();
        
        // Jut for the fun of it, let's color the LCD background based on temperature
        var r = linear(0x00, 0xFF, data_temperature.value, 40);
        var g = linear(0x00, 0x00, data_temperature.value, 40);
        var b = linear(0xFF, 0x00, data_temperature.value, 40);

        lcd.bgColor(r, g, b).cursor(1, 0).print("Temp:" +Math.round(data_temperature.value) + "C");
    }, 500);
    
    // Initialize moisture sensor
    var data_moisture = {"measurename":"Moisture", "unitofmeasure":"%", "value":0};
    var moisture = new five.Sensor("A1");    
    moisture.scale(0, 100).on("change", function() {
        data_moisture.value = this.value;
    });
    
    // Initialize light sensor
    var data_light = {"measurename":"Light", "unitofmeasure":"L", "value":0};
    var light = new five.Sensor("A0");    
    light.on("change", function() {
        data_light.value = this.value;
    });

    // Initialize light sensor
    var data_uv  = {"measurename":"UV", "unitofmeasure":"Index", "value":0};
    var uv = new five.Sensor("A3");    
    uv.on("change", function() {
        data_uv.value = this.value;
    });
    
    // Initialize Button
    var data_button  = {"measurename":"Button", "unitofmeasure":"Down", "value":0};
    var button = new five.Button(6);
    button.on("press", function(){
        data_button.value = 1;
    });
    button.on("release", function(){
        data_button.value = 0;
    });
    
    // Initialize Motion sensor
    var data_motion  = {"measurename":"Motion", "unitofmeasure":"Move", "value":0};
    var motion = new five.Motion(7);
    motion.on("motionstart", function(){ data_motion.value  = 1;});
    motion.on("motioned", function(){ data_motion.value  = 0;});

    // Initialize rotary potentiometer
    var data_rotary = {"measurename":"Potentiometer", "unitofmeasure":"Value", "value":0};
    var rotary = five.Sensor("A2");
    rotary.scale(0,100).on("change", function() { data_rotary.value  = this.value;});
    
    // send data to Azure every second    
    setInterval(function(){
        
        var messages = [
            data_temperature,
            data_humidity,
            data_moisture,
            data_light,
            data_uv,
            data_motion,
            data_button,
            data_rotary
        ];

        // we have several sensors, let's use the bulk messaging option
        connectthedots.send_bulk_message(messages);

        // reset data motion data which gets updated only on 
        data_motion.value = 0;

    }, 1000);
}

// ---------------------------------------------------------------
// Init app

// Init connection to Azure IoT
connectthedots.init_connection(devicesettings)

// Init board
var board = new five.Board({io: new Edison()});

board.on("ready",connect_the_dots);
board.on("message", function(event){
    console.log("Received a %s message, from %s, reporting: %s", event.type, event.class, event.message);
} );
board.on("fail", function(event) {
  console.log("%s sent a 'fail' message: %s", event.class, event.message);
});
board.on("warn", function(event) {
  console.log("%s sent a 'warn' message: %s", event.class, event.message);
});




