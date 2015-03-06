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

#pragma once

#ifndef BUZZERALARMCOMMAND_H_
#define BUZZERALARMCOMMAND_H_

#define BUZZER_COMMAND_NAME	L"beep"

#include "Commands.h"
#include "grove\GroveBuzzer.h"

class BuzzerAlarmCommand : public ICommand {
public:
	BuzzerAlarmCommand(int buzzer_pin);
	virtual ~BuzzerAlarmCommand();
	virtual void invoke(JSONValue *parameters);
	virtual void check();
	virtual std::wstring const &name() const;
private:
	grove::GroveBuzzer	m_buzzer;
	std::wstring		m_name;
	int					m_buzzer_pin;
	long long	m_freq;
	long long	m_delay;
	bool				m_playing;
};

#endif // BUZZERALARMCOMMAND_H_