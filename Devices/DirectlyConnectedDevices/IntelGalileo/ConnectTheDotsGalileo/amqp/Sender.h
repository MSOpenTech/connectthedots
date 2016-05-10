#pragma once

#ifndef _AMQP_SENDER_
#define _AMQP_SENDER_

#include <string>
#include <time.h>
#include <proton\messenger.h>
#include <proton\types.h>
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

#include <memory>
#include <locale>
#include <arduino.h>
#include "IMessage.h"
#include "Address.h"
#include "AMQPData.h"

namespace amqp {

	class Sender {
	public:
		Sender(bool trackMessages = false);
		Sender(std::string const &name, bool trackMessages = false);
		virtual ~Sender();

		/// method sends message to AMQP broker
		void send(IMessage const &message, Address const &address);

		/// method returns true if there is an error
		bool isError() const;

		/// method returns true if tracking was enabled
		bool isTraking() const;

		void enableTracking();
		
	private:
		pn_messenger_t *m_messenger = NULL;
		pn_tracker_t m_tracker;
		std::string m_name;
		bool m_trackMessages;

		/// method starts messenger 
		void _startMessenger();

		/// method formats message properties
		void _addPropertiesToMessage(pn_message_t *pnmessage, IMessage const &message);

		/// method formats message annotations
		void _addAnnotationsToMessage(pn_message_t *pnmessage, IMessage const &message);

		/// method adds meta information to the message header
		void _addMetaToMessage(pn_message_t *pnmessage, IMessage const &message);
		
		/// method serializes message data
		void _serializeMessageMeta(pn_data_t *pndata, std::shared_ptr<IAMQPData> const &data);
		
		/// method verifies the message status
		void _checkTracking();

		/// method throws an error with text details
		void _throwError();
	};

};

#endif // _AMQP_SENDER_