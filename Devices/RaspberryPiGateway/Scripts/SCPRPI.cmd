@echo off
set puttydir=c:\software\putty\
set prjdir=..\
rem set rpi_ip=raspberrypi
set rpi_ip=10.121.204.230
set rpi_usr=pi
set rpi_pw=raspberry

echo Creating RaspberryPiGateway directory
echo mkdir RaspberryPiGateway > %temp%\rpigatewaymkdir.tmp
%puttydir%putty %rpi_usr%@%rpi_ip% -pw %rpi_pw% -m %temp%\rpigatewaymkdir.tmp

echo Copying Gateway and logging files
%puttydir%pscp -pw %rpi_pw% %prjdir%bin\Release\RaspberryPiGateway.exe %rpi_usr%@%rpi_ip%:RaspberryPiGateway/
%puttydir%pscp -pw %rpi_pw% %prjdir%bin\Release\Amqp.Net.dll %rpi_usr%@%rpi_ip%:RaspberryPiGateway/
%puttydir%pscp -pw %rpi_pw% %prjdir%bin\Release\Newtonsoft.Json.dll %rpi_usr%@%rpi_ip%:RaspberryPiGateway/
%puttydir%pscp -pw %rpi_pw% %prjdir%bin\Release\NLog.config %rpi_usr%@%rpi_ip%:RaspberryPiGateway/
%puttydir%pscp -pw %rpi_pw% %prjdir%bin\Release\NLog.dll %rpi_usr%@%rpi_ip%:RaspberryPiGateway/
%puttydir%pscp -pw %rpi_pw% %prjdir%Scripts\autorun.sh %rpi_usr%@%rpi_ip%:RaspberryPiGateway/

echo Marking autorun.sh as executable
echo chmod 755 RaspberryPiGateway/autorun.sh > %temp%\rpigatewayautorunx.tmp
%puttydir%putty %rpi_usr%@%rpi_ip% -pw %rpi_pw% -m %temp%\rpigatewayautorunx.tmp
