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

#ifndef _AMQP_IAMQPDATA_H_
#define _AMQP_IAMQPDATA_H_

#include <proton\message.h>
#include <proton\types.h>
#include <memory>

namespace amqp
{

	class IAMQPData {
	public:
		enum Type {AMQP_STRING, AMQP_UUID, AMQP_SYMBOL};

		virtual Type getDataType() const = 0;
		virtual std::shared_ptr<IAMQPData> copy() const = 0;

		virtual ~IAMQPData() 
		{
		};
	protected:
		IAMQPData() 
		{
		};
	private:
	};

	class AMQPDataComparator
	{
		virtual bool operator () (IAMQPData const &key1, IAMQPData const &key2) const
		{
			return true;
		}
	};
};

#endif // _AMQP_IAMQPDATA_H_