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

#include "BuzzerAlarmCommand.h"

BuzzerAlarmCommand::BuzzerAlarmCommand(int buzzer_pin)
	: m_buzzer(grove::GroveBuzzer(buzzer_pin)),
	m_buzzer_pin(buzzer_pin),
	m_name(BUZZER_COMMAND_NAME),
	m_playing(false),
	m_freq(0),
	m_delay(0)
{
	m_buzzer.turnOff();
}

BuzzerAlarmCommand::~BuzzerAlarmCommand()
{
	m_buzzer.turnOff();
}

void BuzzerAlarmCommand::invoke(JSONValue *parameters)
{
	if (parameters->HasChild(L"freq"))
		m_freq = static_cast<long long>(parameters->Child(L"freq")->AsNumber());
	if (parameters->HasChild(L"delay"))
		m_delay = static_cast<long long>(parameters->Child(L"delay")->AsNumber());
	m_playing = true;
}

void BuzzerAlarmCommand::check()
{
	if (m_delay <= 0)
		m_playing = false;

	if (m_playing) {
		m_delay -= m_freq;
		m_buzzer.beep(m_freq);
	}
}

std::wstring const &BuzzerAlarmCommand::name() const
{
	return (m_name);
}