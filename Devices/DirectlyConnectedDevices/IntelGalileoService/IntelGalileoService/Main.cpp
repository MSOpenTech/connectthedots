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
#include "arduino.h"
#include "json\JSON.h"
#include "Configuration.h"
#include "AllCommands.h"
#include "amqp\Address.h"
#include "amqp\Receiver.h"

int _tmain(int argc, _TCHAR* argv[])
{
    return RunArduinoSketch();
}

amqp::Address	topicAddress;
amqp::Receiver	receiver;
Configuration	config;
Commands		commands;

void setup()
{
	config.load("IntelGalileoService.exe.config");

	topicAddress.setHost(config.host());
	topicAddress.setUser(config.user());
	topicAddress.setPassword(config.password());
	topicAddress.setPath(config.topicName() + "/Subscriptions/" + config.subscription());

	receiver.Subscribe(topicAddress);

	commands.addCommand(new BuzzerAlarmCommand(D4));
	commands.addCommand(new LightCommand(D3));
	commands.addCommand(new ScreenTextCommand());
}

// the loop routine runs over and over again forever:
void loop()
{
	std::string message;

	if (receiver.ReceiveAsync(message)) {
		if (message[0] == '"') { // messages are coming inside quotes ""
			std::string unqouted_message(message.begin() + 1, message.end() - 1);
			message.assign(unqouted_message);
		}

		Log("Received message: %s\n", message.c_str());
		try {
			JSONValue *message_object = JSON::Parse(message.c_str());
			if (message_object != NULL && message_object->HasChild(L"command")) {
				commands.invoke(message_object->Child(L"command")->AsString(), message_object);
			} else {
				throw std::exception("Bad format");
			}
		}
		catch (std::exception e) {
			Log("ERROR: %s\n", e.what());
		}
	}

	commands.checkAll();
}