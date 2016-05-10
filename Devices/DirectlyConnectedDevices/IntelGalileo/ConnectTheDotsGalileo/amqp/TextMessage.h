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

#ifndef _AMQP_TEXTMESSAGE_H_
#define _AMQP_TEXTMESSAGE_H_

#include <locale>
#include <codecvt>
#include "IMessage.h"

namespace amqp
{
	class TextMessage : public IMessage {
	public:

		TextMessage(std::string const &subject, std::wstring const &message, 
			etEncoding encoding = UTF16,
			std::string const &contentType = "text/plain");
		TextMessage(std::string const &subject, std::string const &message,
			etEncoding encoding = UTF8,
			std::string const &contentType = "text/plain");
		virtual ~TextMessage();

		virtual void addAnnotation(IAMQPData const &key, IAMQPData const &value);
		virtual void addProperty(IAMQPData const &key, IAMQPData const &value);

		virtual std::string const &getContentType() const;
		virtual char const *getBytes() const;
		virtual size_t getSize() const;
		virtual std::string const &getSubject() const;
		virtual IMessage::etEncoding getEncoding() const;

		virtual MessageMeta const &getProperties() const;
		virtual MessageMeta const &getAnnotations() const;

		virtual void setContentType(std::string const &contentType);
		virtual void setSubject(std::string const &subject);
		virtual void setMessage(std::wstring const &message, 
								etEncoding encoding = UTF16);
		virtual void setMessage(std::string const &message,
								etEncoding encoding = UTF8);

	private:
		MessageMeta m_properties;
		MessageMeta m_annotations;
		std::wstring m_messageW;
		std::string m_message;
		std::string m_subject;
		std::string m_contentType;
		etEncoding m_encoding;
	};
};

#endif // _AMQP_TEXTMESSAGE_H_