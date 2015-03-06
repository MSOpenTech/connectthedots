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

#ifndef SCREENTEXTCOMMAND_H_
#define SCREENTEXTCOMMAND_H_

#define LCD_WIDTH		16
#define LCD_HEIGHT		2
#define DEFAULT_RED		255
#define DEFAULT_GREEN	255
#define DEFAULT_BLUE	255
#define SCREEN_COMMAND_NAME	L"writetextonscreen"

#include "Commands.h"
#include "grove\rgb_lcd.h"

class ScreenTextCommand : public ICommand {
public:
	ScreenTextCommand();
	virtual ~ScreenTextCommand();
	virtual void invoke(JSONValue *parameters);
	virtual void check();
	virtual std::wstring const &name() const;
private:
	rgb_lcd			m_screen;
	std::wstring	m_name;
	unsigned char	m_red;
	unsigned char	m_green;
	unsigned char	m_blue;
};

#endif // SCREENTEXTCOMMAND_H_