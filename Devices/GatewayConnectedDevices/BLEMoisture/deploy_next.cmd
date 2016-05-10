@echo off
REM //  ---------------------------------------------------------------------------------
REM //  Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.
REM // 
REM //  The MIT License (MIT)
REM // 
REM //  Permission is hereby granted, free of charge, to any person obtaining a copy
REM //  of this software and associated documentation files (the "Software"), to deal
REM //  in the Software without restriction, including without limitation the rights
REM //  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
REM //  copies of the Software, and to permit persons to whom the Software is
REM //  furnished to do so, subject to the following conditions:
REM // 
REM //  The above copyright notice and this permission notice shall be included in
REM //  all copies or substantial portions of the Software.
REM // 
REM //  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
REM //  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
REM //  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
REM //  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
REM //  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
REM //  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
REM //  THE SOFTWARE.
REM //  ---------------------------------------------------------------------------------

set puttydir="C:\software\putty\"
set prjdir=..\..\Gateways\GatewayService\
set scdir=%prjdir%Scripts\ScriptConverter\bin\
set rpi_ip=10.121.204.139
set rpi_usr=pi
set rpi_pw=raspberry
set Configuration=Release
set GW_Home=ctdgtwy
set Staging=%GW_Home%/staging
set PUTTY_CMD=%puttydir%putty %rpi_usr%@%rpi_ip% -pw %rpi_pw% 
set PSCP_CMD=%puttydir%pscp -pw %rpi_pw% 

echo editing line endings for Pi
%scdir%%Configuration%\ScriptConverter.exe "autorun2.sh" 

echo Copying file that starts up python script to read hydrology sensors and format as JSON
%PSCP_CMD% SensorAgent.py  %rpi_usr%@%rpi_ip%:%Staging%/
%PSCP_CMD% BLEMoistureSensor.py  %rpi_usr%@%rpi_ip%:%Staging%/
%PSCP_CMD% SensorAgentConfig.csv  %rpi_usr%@%rpi_ip%:%Staging%/
%PSCP_CMD% DeviceConfig.csv  %rpi_usr%@%rpi_ip%:%Staging%/
%PSCP_CMD% Modified\autorun2.sh  %rpi_usr%@%rpi_ip%:%Staging%/

echo Marking autorun2.sh as executable
del /f %temp%\rpigatewayautorunx.tmp
echo chmod 755 %Staging%/autorun2.sh    	>> %temp%\rpigatewayautorunx.tmp
%PUTTY_CMD% -m                                    %temp%\rpigatewayautorunx.tmp