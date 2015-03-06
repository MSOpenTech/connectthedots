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

#include "LightCommand.h"

LightCommand::LightCommand(int light_pin) 
	: m_light_pin(light_pin),
	m_light_led(grove::Led(light_pin)),
	m_name(LIGHT_COMMAND_NAME)
{

}

LightCommand::~LightCommand()
{
	m_light_led.turnOff();
}

void LightCommand::invoke(JSONValue *parameters)
{
	if (!parameters->HasChild(L"state"))
		throw std::exception("Paremeter /State/ not found");

	if (parameters->Child(L"state")->AsString() == L"on") {
		m_light_led.turnOn();
	} else {
		m_light_led.turnOff();
	}
}

void LightCommand::check()
{

}

std::wstring const &LightCommand::name() const
{
	return (m_name);
}