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
	doc.parse<0>(xmlFile.data());

	// parse for settings
	xml_node<> *node = doc.first_node("configuration")->first_node("appSettings")->first_node("add");
	while (node)
	{
		_parseAttributes(node->first_attribute("key")->value(), node->first_attribute("value")->value());
		node = node->next_sibling("add");
	}
}

void Configuration::_parseAttributes(char* key, char* value)
{
	if (!strcmp(key, "Host"))  { m_host.assign(value); }
	if (!strcmp(key, "User")) { m_user.assign(value);  }
	else if (!strcmp(key, "Password")) { m_password.assign(value); }
	else if (!strcmp(key, "TopicName")) { m_topic_name.assign(value); }
	else if (!strcmp(key, "Subscription")) { m_subscription.assign(value); }
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

std::string const &Configuration::topicName() const
{
	return (m_topic_name);
}

std::string const &Configuration::subscription() const
{
	return (m_subscription);
}
