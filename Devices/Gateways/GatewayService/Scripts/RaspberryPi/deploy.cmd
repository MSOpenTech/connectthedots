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

set puttydir="C:\Program Files (x86)\PuTTY\"
set prjdir=..\..\
set rpi_ip=xxx.xxx.xxx.xxx
set rpi_usr=pi
set rpi_pw=raspberry
set Configuration=Release
set GW_Home=ctdgtwy
set Staging=%GW_Home%/staging
set PUTTY_CMD=%puttydir%putty %rpi_usr%@%rpi_ip% -pw %rpi_pw% 
set PSCP_CMD=%puttydir%pscp -pw %rpi_pw% 

echo editing line endings for Pi
%prjdir%Scripts\ScriptConverter\bin\%Configuration%\ScriptConverter.exe "..\RaspberryPi\certificate_update.sh" "..\RaspberryPi\autorun_install.sh" "..\RaspberryPi\kill_all.sh" "..\RaspberryPi\deploy_and_start_ctd_on_boot.sh"

echo Creating GatewayService directory
del /f %temp%\gatewayservicemkdir.tmp
echo rm -rf %Staging%  >> %temp%\gatewayservicemkdir.tmp
echo rm -rf %GW_Home%  >> %temp%\gatewayservicemkdir.tmp
echo mkdir  %GW_Home%  >> %temp%\gatewayservicemkdir.tmp
echo mkdir  %Staging%  >> %temp%\gatewayservicemkdir.tmp
%PUTTY_CMD% -m %temp%\gatewayservicemkdir.tmp

echo Copying Gateway files
%PSCP_CMD% %prjdir%WindowsService\bin\%Configuration%\*.*                                                       %rpi_usr%@%rpi_ip%:%Staging%/
%PSCP_CMD% %prjdir%DeviceAdapters\SerialPort\bin\%Configuration%\Microsoft.ConnectTheDots.SerialPortAdapter.dll %rpi_usr%@%rpi_ip%:%Staging%/
%PSCP_CMD% %prjdir%DeviceAdapters\Socket\bin\%Configuration%\Microsoft.ConnectTheDots.SocketAdapter.dll         %rpi_usr%@%rpi_ip%:%Staging%/
%PSCP_CMD% %prjdir%DeviceAdapters\Bluetooth\bin\%Configuration%\Microsoft.ConnectTheDots.BluetoothUARTAdapter.dll   %rpi_usr%@%rpi_ip%:%Staging%/
REM %PSCP_CMD% %prjdir%Tests\SocketServiceDeviceMock\bin\%Configuration%\SocketDeviceMock.exe                       %rpi_usr%@%rpi_ip%:%Staging%/
REM %PSCP_CMD% %prjdir%Tests\DeviceAdapterTestMock\bin\%Configuration%\DataAdapterTestMock.dll                      %rpi_usr%@%rpi_ip%:%Staging%/
REM %PSCP_CMD% %prjdir%Tests\CoreTest\bin\%Configuration%\CoreTest.exe                                              %rpi_usr%@%rpi_ip%:%Staging%/

echo copying scripts
%PSCP_CMD% %prjdir%Scripts\RaspberryPi\Modified\certificate_update.sh			%rpi_usr%@%rpi_ip%:%Staging%/
%PSCP_CMD% %prjdir%Scripts\RaspberryPi\Modified\autorun_install.sh				%rpi_usr%@%rpi_ip%:%Staging%/
%PSCP_CMD% %prjdir%Scripts\RaspberryPi\Modified\kill_all.sh					    %rpi_usr%@%rpi_ip%:%Staging%/
%PSCP_CMD% %prjdir%Scripts\RaspberryPi\Modified\deploy_and_start_ctd_on_boot.sh	%rpi_usr%@%rpi_ip%:%GW_Home%/

echo Marking autorun_once.sh and autorun_install.sh as executable
del /f %temp%\rpigatewayautorunx.tmp
echo chmod 755 %Staging%/certificate_update.sh					>> %temp%\rpigatewayautorunx.tmp
echo chmod 755 %Staging%/autorun_install.sh						>> %temp%\rpigatewayautorunx.tmp
echo chmod 755 %Staging%/kill_all.sh							>> %temp%\rpigatewayautorunx.tmp
echo chmod 755 %GW_Home%/deploy_and_start_ctd_on_boot.sh		>> %temp%\rpigatewayautorunx.tmp
%PUTTY_CMD% -m													   %temp%\rpigatewayautorunx.tmp

echo Run deploy_next.sh for any supplementary sensor files
