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

#include "stdafx.h"
#include <arduino.h>
#include "sensors\Sensors.h"
#include "json.h"
#include "Configuration.h"
#include "amqp\amqp.h"


#define CONFIG_FILE_PATH		"ConnectTheDotsGalileo.exe.config"
#define REFRESH_DATA_INTERVAL	500 // milliseconds

amqp::Address	eventHubAddress;
amqp::Sender	amqpSender;
Configuration	config;
Sensors			sensors;

int _tmain(int argc, _TCHAR* argv[])
{
	return RunArduinoSketch();
}

void setup()
{
	_CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);

	try {
		config.load(CONFIG_FILE_PATH);
		eventHubAddress.setAddress(config.host(),
			config.user(),
			config.password(),
			config.eventHubName());

		if (config.amqpMessageTracking()) {
			amqpSender.enableTracking();
		}

		// sensors willn't be initialized until value request
		// we can add all pointers to the list at the begining even if sensors haven't connected
		sensors.addSensor(new FakeSensor());
		sensors.addSensor(new GroveLightSensor(A1));
		sensors.addSensor(new GroveTemperatureSensor(A0));
		sensors.addSensor(new WeatherShieldHumiditySensor());
		sensors.addSensor(new WeatherShieldPressureSensor());
		sensors.addSensor(new WeatherShieldTemperatureSensor());
		sensors.addSensor(new OnBoardTemperatureSensor());
	}
	catch (std::exception &e) {
		Log(e.what());
	}
}

void loop()
{
	try {
		Configuration::SensorsSettings::const_iterator	it;
		for (it = config.sensors().begin(); it != config.sensors().end(); ++it) {

			// Create JSON messsage payload
			JSONObject jsonData;

			jsonData[L"unitofmeasure"] = new JSONValue(it->unit_of_measure);
			jsonData[L"measurename"] = new JSONValue(it->measure_name);
			jsonData[L"location"] = new JSONValue(config.location());
			jsonData[L"organization"] = new JSONValue(config.organization());
			jsonData[L"guid"] = new JSONValue(config.guidW());
			jsonData[L"displayname"] = new JSONValue(config.deviceNameW());
			jsonData[L"value"] = new JSONValue(sensors.value(it->sensor));
			jsonData[L"subject"] = new JSONValue(config.subjectW());
			jsonData[L"timecreated"] = new JSONValue(config.getTimeNow());

			amqp::JsonMessage telemetryMessage(config.subject(), jsonData, amqp::IMessage::UTF8);

			// The AMQP Prton lib requires the partition-key to be 16 bytes max
			telemetryMessage.addAnnotation(amqp::AMQPSymbol("x-opt-partition-key"), amqp::AMQPuuid(config.guid().substr(0, 16)));

			// Send AMQP message to Azure Event Hub for each sensor
			amqpSender.send(telemetryMessage, eventHubAddress);

		}

		Sleep(REFRESH_DATA_INTERVAL);
	}
	catch (std::exception &e) 
	{
		Log(e.what());
	}
}