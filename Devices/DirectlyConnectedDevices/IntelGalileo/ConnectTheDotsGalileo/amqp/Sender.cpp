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

#include "Sender.h"

amqp::Sender::Sender(bool trackMessages)
	: m_trackMessages(trackMessages)
{
	m_messenger = pn_messenger(NULL);
	_startMessenger();
}

amqp::Sender::Sender(std::string const &name, bool trackMessages) 
	: m_name(name), m_trackMessages(trackMessages)
{
	m_messenger = pn_messenger(m_name.c_str());
	_startMessenger();
}

amqp::Sender::~Sender()
{
	if (m_messenger != NULL) {
		pn_messenger_stop(m_messenger);
		pn_messenger_free(m_messenger);
	}
}

void amqp::Sender::_startMessenger()
{
	pn_messenger_set_outgoing_window(m_messenger, 5);
	pn_messenger_start(m_messenger);
}

void amqp::Sender::send(IMessage const &message, Address const &address)
{
	pn_data_t *pnmessage_body = NULL;
	pn_message_t *pnmessage = pn_message();

	if (pnmessage == NULL) {
		throw std::exception("ERROR: Message could not be created.");
	}

	pn_message_set_address(pnmessage, address.toString().c_str());
	_addMetaToMessage(pnmessage, message);

	pnmessage_body = pn_message_body(pnmessage);
	pn_data_put_binary(pnmessage_body, pn_bytes(message.getSize(), message.getBytes()));
	pn_messenger_put(m_messenger, pnmessage);

	if (isError()) {
		_throwError();
	}

	// To avoid traffic flud and speed up the solution better to use blocking scokets in tracking mode
	if (isTraking()) {
		Log("Sending messages to %s\n", address.toString().c_str());
		m_tracker = pn_messenger_outgoing_tracker(m_messenger);
		pn_messenger_send(m_messenger, -1); // sync
	} 
	else {
		pn_messenger_send(m_messenger, 1); // async
	}

	if (isError()) {
		_throwError();
	}

	_checkTracking();

	pn_message_free(pnmessage);
}

void amqp::Sender::_throwError()
{
	throw std::exception(pn_error_text(pn_messenger_error(m_messenger)));
}

void amqp::Sender::_addMetaToMessage(pn_message_t *pnmessage, IMessage const &message)
{
	pn_timestamp_t utcTime;
	time(&utcTime);

	_addPropertiesToMessage(pnmessage, message);
	_addAnnotationsToMessage(pnmessage, message);

	pn_message_set_content_type(pnmessage, message.getContentType().c_str());
	pn_message_set_inferred(pnmessage, true);
	pn_message_set_subject(pnmessage, message.getSubject().c_str());
	pn_message_set_ttl(pnmessage, 86400000);
	pn_message_set_creation_time(pnmessage, utcTime);
	switch (message.getEncoding())
	{
	case IMessage::UTF8: 
		pn_message_set_content_encoding(pnmessage, "UTF-8");
		break;
	case IMessage::UTF16:
		pn_message_set_content_encoding(pnmessage, "UTF-16");
		break;
	}
	
}

void amqp::Sender::_addPropertiesToMessage(pn_message_t *pnmessage, IMessage const &message)
{
	pn_data_t *pnmessage_properties = pn_message_properties(pnmessage);
	pn_data_put_map(pnmessage_properties);
	pn_data_enter(pnmessage_properties);

	MessageMeta::const_iterator i = message.getProperties().begin();
	for (; i != message.getProperties().end(); ++i) {
		_serializeMessageMeta(pnmessage_properties, (*i));
	}

	pn_data_exit(pnmessage_properties);
}

void amqp::Sender::_addAnnotationsToMessage(pn_message_t *pnmessage, IMessage const &message)
{
	pn_data_t *pnmessage_annotations = pn_message_annotations(pnmessage);
	pn_data_put_map(pnmessage_annotations);
	pn_data_enter(pnmessage_annotations);

	MessageMeta::const_iterator i = message.getAnnotations().begin();
	for (; i != message.getAnnotations().end(); ++i) {
		_serializeMessageMeta(pnmessage_annotations, (*i));
	}

	pn_data_exit(pnmessage_annotations);
}

void amqp::Sender::_serializeMessageMeta(pn_data_t *pndata, std::shared_ptr<amqp::IAMQPData> const &data)
{
		switch (data->getDataType())
		{
		case IAMQPData::AMQP_STRING: 
			pn_data_put_string(pndata, (std::dynamic_pointer_cast<AMQPString const>(data))->getString());
			break;
		case IAMQPData::AMQP_SYMBOL:
			pn_data_put_symbol(pndata, (std::dynamic_pointer_cast<AMQPSymbol const>(data))->getSymbol());
			break;
		case IAMQPData::AMQP_UUID:
			pn_data_put_uuid(pndata, (std::dynamic_pointer_cast<AMQPuuid const>(data))->getUUID());
			break;
		default:
			throw std::exception("ERROR: wrong data type");
		}
}

void amqp::Sender::_checkTracking()
{
	if (!isTraking()) 
		return;

	pn_status_t status = PN_STATUS_UNKNOWN;

	status = pn_messenger_status(m_messenger, m_tracker);

	switch (status)
	{
		case PN_STATUS_UNKNOWN:
			Log("Message status PN_STATUS_UNKNOWN\n");
			break;

		case PN_STATUS_PENDING:
			Log("Message status PN_STATUS_PENDING\n");
			break;

		case PN_STATUS_ACCEPTED:
			Log("Message status PN_STATUS_ACCEPTED\n");
			break;

		case PN_STATUS_REJECTED:
			Log("Message status PN_STATUS_REJECTED\n");
			break;

		case PN_STATUS_MODIFIED:
			Log("Message status PN_STATUS_MODIFIED\n");
			break;

		case PN_STATUS_RELEASED:
			Log("Message status PN_STATUS_RELEASED\n");
			break;

		case PN_STATUS_ABORTED:
			Log("Message status PN_STATUS_ABORTED\n");
			break;

		case PN_STATUS_SETTLED:
			Log("Message status PN_STATUS_SETTLED\n");
			break;

		default:
			Log("Message status UNRECOGNIZED (%d)\n", (int)status);
			break;
	}

	Log("Final send status is: ");
	if (PN_STATUS_ACCEPTED == status)
	{
		Log("successful!\n");
	}
	else if (PN_STATUS_REJECTED == status)
	{
		Log("rejected by the broker\n");
	}
	else if (PN_STATUS_PENDING == status)
	{
		Log("Giving up, assuming send failed\n");
	}
	else if (PN_STATUS_ABORTED == status)
	{

		Log("failed, never sent on network\n");
	}
	else
	{
		Log("unclear\n");
	}

	Log("CALL pn_messenger_settle... ");
	int err = pn_messenger_settle(m_messenger, m_tracker, PN_CUMULATIVE);
	Log("RETURNED %d\n", err);
	if (err != 0)
	{
		_throwError();
	}
}

bool amqp::Sender::isError() const
{
	return (pn_messenger_errno(m_messenger) != 0);
}

bool amqp::Sender::isTraking() const
{
	return (m_trackMessages);
}

void amqp::Sender::enableTracking()
{
	m_trackMessages = true;
}