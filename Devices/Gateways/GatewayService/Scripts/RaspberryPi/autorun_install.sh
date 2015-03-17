#  ---------------------------------------------------------------------------------
#  Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.
# 
#  The MIT License (MIT)
# 
#  Permission is hereby granted, free of charge, to any person obtaining a copy
#  of this software and associated documentation files (the "Software"), to deal
#  in the Software without restriction, including without limitation the rights
#  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
#  copies of the Software, and to permit persons to whom the Software is
#  furnished to do so, subject to the following conditions:
# 
#  The above copyright notice and this permission notice shall be included in
#  all copies or substantial portions of the Software.
# 
#  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
#  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
#  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
#  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
#  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
#  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
#  THE SOFTWARE.
#  ---------------------------------------------------------------------------------
#!/bin/bash


#
# the standard account for a Raspberry pi board is 'pi'
# please change as needed across code base
#
export GW_ACCOUNT_HOME=/home/pi
export GW_HOME=$GW_ACCOUNT_HOME/ctdgtwy
export LOGS=$GW_HOME/logs

echo "$(date) => autorun.sh: starting autorun.sh" >> $GW_HOME/boot_sequence.log
#

echo "Starting supplementary sensor script if present"
echo "$(date) => autorun.sh: calling supplementary startup script autorun2.sh if necessary" >> $GW_HOME/boot_sequence.log
$GW_HOME/autorun2.sh &
#

#
# Start monitoring gateway process
# Event log entries will be written to /var/lib/mono/EventLog/Application
#
echo "Setting MONO_EVENTLOG_TYPE to local"
export MONO_EVENTLOG_TYPE=local
echo "Monitoring Gateway"
LOG=monitor_$(date +"%m-%d-%Y-%T").log
MONITORED="GatewayService"
PERIOD=5
DELETE_LOCK="sudo rm -f /tmp/Microsoft.ConnectTheDots.GatewayService.exe.lock"	
RESTART="sudo /usr/bin/mono-service $GW_HOME/Microsoft.ConnectTheDots.GatewayService.exe"


#
# Consider using debug mode for mono 
#
#		export MONO_LOG_LEVEL=debug 
#		/usr/bin/mono-service $GW_HOME/Microsoft.ConnectTheDots.GatewayService.exe --debug > monoOutput.txt &
#

echo Starting gateway from directory $(pwd)
export MONO_PATH=$GW_HOME
echo MONO_PATH is $MONO_PATH

echo "$(date) => autorun.sh: starting permanent while loop" >> $GW_HOME/boot_sequence.log
while :
do
	 test `ps ax | grep $MONITORED | awk '{ print $1;}' | wc | awk '{ print $1;}'` -gt 1 && RUNNING=1 || RUNNING=0
	 test $RUNNING -eq 0 && echo "$(date) => Restarting $MONITORED..." >> $LOGS/$LOG && $DELETE_LOCK && $RESTART || echo "$(date) => $MONITORED is running..." >> $LOGS/$LOG
	 sleep $PERIOD
done

echo "$(date) => autorun.sh: finished autorun.sh" >> $GW_HOME/boot_sequence.log