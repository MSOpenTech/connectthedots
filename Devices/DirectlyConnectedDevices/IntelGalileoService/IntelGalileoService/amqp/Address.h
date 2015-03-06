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

#ifndef _AMQP_ADDRESS
#define _AMQP_ADDRESS

#include <string>

namespace amqp {

	class Address 
	{
	public:

		Address(std::string const &host = "", 
			std::string const &user = "",
			std::string const &password = "",
			std::string const &path = "",
			int port = 5671,
			std::string const &scheme = "amqps");

		virtual ~Address()
		{
		};

		std::string toString() const;

		void setAddress(std::string const &host,
			std::string const &user,
			std::string const &password,
			std::string const &path,
			int port = 5671,
			std::string const &scheme = "amqps");

		void setHost(std::string const &host);
		void setUser(std::string const &user);
		void setPassword(std::string const &password);
		void setPath(std::string const &path);
		void setPort(int port);
		void setScheme(std::string const &scheme);

		std::string const &getHost() const;
		std::string const &getUser() const;
		std::string const &getPassword() const;
		std::string const &getPath() const;
		int getPort() const;
		std::string const &getScheme() const;

	private:
		std::string m_host;
		std::string m_user;
		std::string m_password;
		std::string m_path;
		std::string m_scheme;
		int m_port;
	};


};

#endif // _AMQP_ADDRESS