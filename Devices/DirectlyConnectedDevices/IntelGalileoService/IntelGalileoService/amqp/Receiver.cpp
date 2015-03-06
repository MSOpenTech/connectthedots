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

#include "Receiver.h"

amqp::Receiver::Receiver()
{
	m_messenger = pn_messenger(NULL);
	pn_messenger_set_incoming_window(m_messenger, 1);
	pn_messenger_set_blocking(m_messenger, false);
}


amqp::Receiver::~Receiver()
{
	if (!pn_messenger_stopped(m_messenger))
		pn_messenger_stop(m_messenger);
	pn_messenger_free(m_messenger);
}

/**
* Subcribing to a channel
* @param[in] address an address of a channel
*/
void amqp::Receiver::Subscribe(Address const &address)
{
	m_subscribtionAddress = address.toString();
	Log("Subscribing to %s\n", m_subscribtionAddress.c_str());

	pn_subscription_t *subsribtion = pn_messenger_subscribe(m_messenger, m_subscribtionAddress.c_str());

	if (pn_messenger_errno(m_messenger) != 0) {
		Log("ERROR: %s\n", pn_error_text(pn_messenger_error(m_messenger)));
	}

	pn_messenger_start(m_messenger);
}

/**
* Check if there is a message in the incoming queue and receive it
* 
* @param[out] commandString a buffer where we'll put the message
* @return true if messagee has been received 
*/
bool amqp::Receiver::ReceiveAsync(std::string &received_message)
{
	pn_message_t	*message	= pn_message();
	char			message_buffer[BUFFER_SIZE];
	size_t			message_buffer_size = BUFFER_SIZE;
	int				recv_error;

	recv_error = pn_messenger_recv(m_messenger, 1);

	if (!recv_error) {
		while (pn_messenger_incoming(m_messenger)) {

			pn_messenger_get(m_messenger, message);
			pn_data_t *body = pn_message_body(message);

			recv_error = pn_data_format(body, message_buffer, &message_buffer_size);
			
			if (recv_error == PN_OVERFLOW) {
				throw std::exception("Receiving buffer overflow");
			}

			received_message.assign(message_buffer);

			pn_tracker_t tracker = pn_messenger_incoming_tracker(m_messenger);
			pn_messenger_accept(m_messenger, tracker, PN_CUMULATIVE);

		}
		pn_message_free(message);
		return (true);
	}

	pn_message_free(message);
	return (false);
}