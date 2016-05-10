'use strict'

// Common logic
var connectthedots = require('connectthedots');
// Settings
var devicesettings = require('./settings.json');

// Sensortag utility class
var worker = require('./sensorWorker.js');

// Init connection to Azure IoT
connectthedots.init_connection(devicesettings);

// Start sensor worker with the send_message callback to the data reception
worker.start(connectthedots.send_message);
