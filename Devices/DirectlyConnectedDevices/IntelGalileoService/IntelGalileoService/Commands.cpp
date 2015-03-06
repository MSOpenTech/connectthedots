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

#include "Commands.h"


Commands::Commands()
{

}

Commands::~Commands()
{

}

void Commands::addCommand(ICommand *command)
{
	if (m_commands.find(command->name()) == m_commands.end()) {
		m_commands[command->name()] = command;
	} else {
		throw std::exception("Command is already in the list");
	}
}

void Commands::invoke(std::wstring const &command_name, JSONValue *parameters)
{
	if (m_commands.find(command_name) != m_commands.end()) {
		m_commands[command_name]->invoke(parameters);
	} else {
		throw std::exception("Command not found");
	}
}

void Commands::checkAll()
{
	commands_list_t::iterator it = m_commands.begin();
	for (; it != m_commands.end(); ++it) {
		it->second->check();
	}
}