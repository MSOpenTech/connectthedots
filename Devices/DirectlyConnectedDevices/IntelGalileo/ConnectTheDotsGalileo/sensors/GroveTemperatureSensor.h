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

#ifndef GROVETEMPERATURESENSOR_H_
#define GROVETEMPERATURESENSOR_H_

#include <string>
#include "ISensor.h"
#include "..\grove\GroveTemperature.h"

#define GROVETEMPERATURESENSOR_NAME "GroveTemperatureSensor"

class GroveTemperatureSensor : public ISensor 
{
public:
	GroveTemperatureSensor(int pin)
		: m_pin(pin), m_name(GROVETEMPERATURESENSOR_NAME)
	{

	}

	virtual ~GroveTemperatureSensor()
	{
		if (m_temperature != NULL)
			delete m_temperature;
	}

	std::string const &name() const
	{
		return (m_name);
	}

	double value()
	{
		if (m_temperature == NULL)
			_init();
		return (static_cast<double>(m_temperature->inF()));
	}
private:
	void _init() {
		m_temperature = new grove::Temperature(m_pin);
	}

	int				m_pin;
	std::string		m_name;
	grove::Temperature	*m_temperature = NULL;
};

#endif // GROVETEMPERATURESENSOR_H_