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

#include "GroveTemperature.h"

/**

\param[in] int Number of an input pin
*/
grove::Temperature::Temperature(int pin)
	: m_pin(pin)
{
	pinMode(m_pin, INPUT);
}

float grove::Temperature::inC()
{
	float sensorValue = static_cast<float>(rawValue());
	float resistance = static_cast<float>((1023.0 - sensorValue) * 10000.0) / sensorValue;
	float temperatureInC = static_cast<float>(1.0 / (log(resistance / 10000.0) / 3975.0 + 1.0 / 298.15) - 273.15);
	return (temperatureInC);
}

float grove::Temperature::inF()
{
	float temperatureInC = inC();
	float temperatureInF = static_cast<float>(temperatureInC * 9.0 / 5.0 + 32.0);
	return (temperatureInF);
}

int grove::Temperature::rawValue()
{
	int sensorValue = analogRead(m_pin);
	return (sensorValue);
}