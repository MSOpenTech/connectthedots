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

#include "Configuration.h"

Configuration::Configuration() 
	: m_amqp_message_tracking(false)
{

}

Configuration::~Configuration()
{

}

void Configuration::load(std::string const &filepath)
{
	Log("Loading configuration ... \n");

	using namespace rapidxml;

	// Get configuration file into a dom object
	file<> xmlFile(filepath.c_str());
	xml_document<> doc;
	sensor_settings_t sensorSettings;

	doc.parse<0>(xmlFile.data());

	// parse for app settings
	xml_node<> *node = doc.first_node("configuration")->first_node("appSettings")->first_node("add");
	while (node)
	{
		_parseAppSettings(node->first_attribute("key")->value(), node->first_attribute("value")->value());
		node = node->next_sibling("add");
	}

	Log("Loading sensor set ... \n");
	// Count number of sensors listed in the settings file
	node = doc.first_node("configuration")->first_node("sensorSettings");
	while (node)
	{
		xml_node<> *sensorNode = node->first_node("add");
		while (sensorNode)
		{
			_parseSensorSettings(sensorNode->first_attribute("key")->value(), sensorNode->first_attribute("value")->value(), sensorSettings);
			sensorNode = sensorNode->next_sibling("add");
		}

		m_sensors.push_back(sensorSettings);
		node = node->next_sibling("sensorSettings");
	}

}

void Configuration::_parseAppSettings(char* key, char* value)
{
	std::wstring_convert< std::codecvt<wchar_t, char, std::mbstate_t> > conv;

	if (!strcmp(key, "Host"))  { m_host.assign(value); }
	else if (!strcmp(key, "User")) { m_user.assign(value); }
	else if (!strcmp(key, "Password")) { m_password.assign(value); }
	else if (!strcmp(key, "EventHubName")) { m_event_hub_name.assign(value); }
	else if (!strcmp(key, "DeviceName")) { m_device_name.assign(value); m_device_nameW = conv.from_bytes(m_device_name); }
	else if (!strcmp(key, "Guid")) { m_guid.assign(value); m_guidW = conv.from_bytes(m_guid); }
	else if (!strcmp(key, "Location")) { m_conv_buffer.assign(value); m_location = conv.from_bytes(m_conv_buffer); }
	else if (!strcmp(key, "Organization")) { m_conv_buffer.assign(value);  m_organization = conv.from_bytes(m_conv_buffer); }
	else if (!strcmp(key, "Subject")) { m_subject.assign(value); m_subjectW = conv.from_bytes(m_subject); }
	else if (!strcmp(key, "AmqpMessageTracking") && !strcmp(value, "false")) { m_amqp_message_tracking = false; }
	else if (!strcmp(key, "AmqpMessageTracking") && !strcmp(value, "true")) { m_amqp_message_tracking = true; }
}

void Configuration::_parseSensorSettings(char* key, char* value, Configuration::sensor_settings_t& settings)
{
	std::wstring_convert< std::codecvt<wchar_t, char, std::mbstate_t> > conv;

	if (!strcmp(key, "measurename"))  { m_conv_buffer.assign(value);  settings.measure_name = conv.from_bytes(m_conv_buffer); }
	else if (!strcmp(key, "unitofmeasure")) { m_conv_buffer.assign(value);  settings.unit_of_measure = conv.from_bytes(m_conv_buffer); }
	else if (!strcmp(key, "sensor")) { settings.sensor.assign(value); }
}

std::string const &Configuration::host() const
{
	return (m_host);
}

std::string const &Configuration::user() const
{
	return (m_user);
}

std::string const &Configuration::password() const
{
	return (m_password);
}

std::string const &Configuration::eventHubName() const
{
	return (m_event_hub_name);
}

std::string const &Configuration::deviceName() const
{
	return (m_device_name);
}

std::wstring const &Configuration::deviceNameW() const
{
	return (m_device_nameW);
}

std::string const &Configuration::subject() const
{
	return (m_subject);
}

std::wstring const &Configuration::subjectW() const
{
	return (m_subjectW);
}

std::wstring const &Configuration::guidW() const
{
	return (m_guidW);
}

std::string const &Configuration::guid() const
{
	return (m_guid);
}

std::wstring const &Configuration::location() const
{
	return (m_location);
}

std::wstring const &Configuration::organization() const
{
	return (m_organization);
}

bool Configuration::amqpMessageTracking() const
{
	return (m_amqp_message_tracking);
}

Configuration::SensorsSettings const &Configuration::sensors() const
{
	return (m_sensors);
}

std::wstring Configuration::getTimeNow() const
{
	// We need to time stamp the AMQP message.
	// Getting UTC time
	std::time_t currentTime = std::time(nullptr);

	std::wstringstream ostr;
	ostr << std::put_time(std::gmtime(&currentTime), L"%Y-%m-%dT%H:%M:%SZ");
	return (ostr.str());
}

