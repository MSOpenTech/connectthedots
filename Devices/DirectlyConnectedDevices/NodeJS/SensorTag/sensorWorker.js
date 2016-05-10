var SensorTag = require('sensortag');
var async = require('async');

var lastSensorTag;
var keepWorking = false;
var dataCallback;

function onErrorRestart(){
  if(keepWorking)
    SensorTag.discover(onDiscover);
}

function onDiscover(sensorTag) {
  console.log('SensorTag discovered', sensorTag.id);

  sensorTag.on('disconnect', function() {
    console.log('SensorTag disconnected', sensorTag.id);
    if(keepWorking)
      SensorTag.discover(onDiscover);
  });
  sensorTag.connectAndSetUp(function(error){
    if(error){
      onErrorRestart();
    }
    else {
      lastSensorTag = sensorTag;

      sensorTag.enableIrTemperature(function (error) { if (error) console.log('enableIrTemperature ' + error); });
      sensorTag.setIrTemperaturePeriod(1000, function (error) { if (error) console.log('setIrTemperaturePeriod ' + error); });
      sensorTag.notifyIrTemperature(function (error) { if (error) console.log('notifyIrTemperature ' + error); });
      sensorTag.on('irTemperatureChange', function (objectTemperature, ambientTemperature) {
        if(dataCallback) dataCallback('Temperature', 'C', ambientTemperature);
      });

      sensorTag.enableHumidity(function (error) { if (error) console.log('enableHumidity ' + error); });
      sensorTag.setHumidityPeriod(1000, function (error) { if (error) console.log('setHumidityPeriod ' + error); });
      sensorTag.notifyHumidity(function (error) { if (error) console.log('notifyHumidity ' + error); });
      sensorTag.on('humidityChange', function (temperature, humidity) {
        if(dataCallback) dataCallback('Humidity', '%', humidity);
      });

      sensorTag.enableLuxometer(function (error) { if (error) console.log('enableLuxometer ' + error); });
      sensorTag.setLuxometerPeriod(1000, function (error) { if (error) console.log('setLuxometerPeriod ' + error); });
      sensorTag.notifyLuxometer(function (error) { if (error) console.log('notifyIrTemperature ' + error); });
      sensorTag.on('luxometerChange', function (lux) {
        if(dataCallback) dataCallback('Light', 'lux', lux);
      });

      sensorTag.enableBarometricPressure(function (error) { if (error) console.log('enableBarometricPressure ' + error); });
      sensorTag.setBarometricPressurePeriod(1000, function (error) { if (error) console.log('setBarometricPressurePeriod ' + error); });
      sensorTag.notifyBarometricPressure(function (error) { if (error) console.log('notifyBarometricPressure ' + error); });
      sensorTag.on('barometricPressureChange', function (pressure) {
        if(dataCallback) dataCallback('Barometric Pressure', 'mHg', pressure);
      });
    }
  });
}

exports.start = function(cb){
  dataCallback = cb;
  SensorTag.discover(onDiscover);
}

exports.stop = function(){
  keepWorking = false;
  dataCallback = null;
  if(lastSensorTag)
    lastSensorTag.disconnect();
}
