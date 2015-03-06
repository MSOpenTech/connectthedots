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

#include "ScreenTextCommand.h"

ScreenTextCommand::ScreenTextCommand()
	: m_name(SCREEN_COMMAND_NAME),
	m_red(DEFAULT_RED),
	m_green(DEFAULT_GREEN),
	m_blue(DEFAULT_BLUE)
{
	m_screen.begin(LCD_WIDTH, LCD_HEIGHT);
	m_screen.setRGB(m_red, m_green, m_blue);
	m_screen.clear();
}

ScreenTextCommand::~ScreenTextCommand()
{
	m_screen.clear();
}

void ScreenTextCommand::invoke(JSONValue *parameters)
{
	if (!parameters->HasChild(L"text"))
		throw std::exception("Bad command format");

	std::wstring wtext = parameters->Child(L"text")->AsString();
	std::string text(wtext.begin(), wtext.end());

	if (parameters->HasChild(L"rgb"))
		swscanf_s(parameters->Child(L"rgb")->AsString().c_str(), L"%d,%d,%d", &m_red, &m_green, &m_blue);

	m_screen.setRGB(m_red, m_green, m_blue);
	m_screen.setCursor(0, 0);
	m_screen.print(text.c_str());
}

void ScreenTextCommand::check()
{

}

std::wstring const &ScreenTextCommand::name() const
{
	return (m_name);
}