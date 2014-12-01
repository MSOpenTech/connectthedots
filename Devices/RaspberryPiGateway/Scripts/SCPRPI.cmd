REM SCPRPI.cmd
@echo off
set puttydir=d:\software\putty\
set prjdir=..\
set rpi_ip=raspberrypi
rem set rpi_ip=192.168.1.107
set rpi_usr=pi
set rpi_pw=raspberry

echo Creating RaspberryPiGateway directory
echo mkdir RaspberryPiGateway > %temp%\rpigatewaymkdir.tmp
%puttydir%putty %rpi_usr%@%rpi_ip% -pw %rpi_pw% -m %temp%\rpigatewaymkdir.tmp

echo Copying Gateway files
%puttydir%pscp -pw %rpi_pw% %prjdir%bin\Debug\RaspberryPiGateway.exe %rpi_usr%@%rpi_ip%:RaspberryPiGateway/
%puttydir%pscp -pw %rpi_pw% %prjdir%bin\Debug\Amqp.Net.dll %rpi_usr%@%rpi_ip%:RaspberryPiGateway/
%puttydir%pscp -pw %rpi_pw% %prjdir%bin\Debug\Newtonsoft.Json.dll %rpi_usr%@%rpi_ip%:RaspberryPiGateway/
%puttydir%pscp -pw %rpi_pw% %prjdir%Scripts\autorun.sh %rpi_usr%@%rpi_ip%:RaspberryPiGateway/autorun.sh

echo Marking autorun.sh as executable
echo chmod 755 RaspberryPiGateway/autorun.sh > %temp%\rpigatewayautorunx.tmp
%puttydir%putty %rpi_usr%@%rpi_ip% -pw %rpi_pw% -m %temp%\rpigatewayautorunx.tmp
