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

#include "AMQPuuid.h"

amqp::AMQPuuid::AMQPuuid(std::string const &uuid)
	: m_uuid(uuid)
{
	
}

pn_uuid_t amqp::AMQPuuid::getUUID() const
{
	pn_uuid_t uuid;
	m_uuid._Copy_s(uuid.bytes, sizeof(uuid.bytes), m_uuid.length());
	return (uuid);
}

amqp::IAMQPData::Type amqp::AMQPuuid::getDataType() const
{
	return (IAMQPData::AMQP_UUID);
}

std::shared_ptr<amqp::IAMQPData> amqp::AMQPuuid::copy() const
{
	return std::shared_ptr<IAMQPData>(new AMQPuuid(m_uuid));
}