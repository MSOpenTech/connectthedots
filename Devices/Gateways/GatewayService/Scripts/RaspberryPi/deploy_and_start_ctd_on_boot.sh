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
export STAGING=$GW_HOME/staging

echo "$(date) => deploy_and_start_ctd_on_boot.sh: started" >> $GW_HOME/boot_sequence.log
#
#if [ -f $GW_HOME/booting.log ];
#then
#	echo "$(date) => deploy_and_start_ctd_on_boot.sh: called from rc.local in boot sequence" >> $GW_HOME/boot_sequence.log
#    # deploy_and_start_ctd_on_boot.sh run from rc.local in boot sequence, nothing should need to be killed
#    # need to delete file so subsequent runs from prompt ok.
#    rm -f $GW_HOME/booting.log
#else
#	echo "$(date) => deploy_and_start_ctd_on_boot.sh: called from command line, not boot sequence" >> $GW_HOME/boot_sequence.log
    # autorun_once.sh run from command prompt, need to kill any pre-running services
	# Kill all mono processes and GatewayService as well, kill the monitoring process that is performing a sleep
	echo "Trying to kill all mono processes..."
	for KILLPID in `ps axo pid,ppid,cmd | grep -i 'mono'           | awk '{ print $1;}'`; do sudo kill -9 $KILLPID; done
	for KILLPID in `ps axo pid,ppid,cmd | grep -i 'gatewayservice' | awk '{ print $1;}'`; do sudo kill -9 $KILLPID; done
	for KILLPID in `ps axo pid,ppid,cmd | grep -i 'sleep'          | awk '{ print $2;}'`; do sudo kill -9 $KILLPID; done
#fi

echo "Trying to delete lock file if there is any..."
sudo rm -f /tmp/Microsoft.ConnectTheDots.GatewayService.exe.lock

echo updating files...
find $GW_HOME/ -maxdepth 1 \! -name 'deploy_and_start_ctd_on_boot.sh' -type f -delete

cp $STAGING/* $GW_HOME/
echo "$(date) => deploy_and_start_ctd_on_boot: creating autorun.sh from template" >> $GW_HOME/boot_sequence.log
rm $GW_HOME/autorun.sh
mv $GW_HOME/autorun_install.sh $GW_HOME/autorun.sh

echo "$(date) => deploy_and_start_ctd_on_boot: calling autorun.sh" >> $GW_HOME/boot_sequence.log
echo "Starting host processes..."
sudo $GW_HOME/autorun.sh

echo "$(date) => deploy_and_start_ctd_on_boot: finished" >> $GW_HOME/boot_sequence.log
#
# Add the below line to /etc/rc.local
#
# the standard account for a Raspberry pi board is 'pi'
# please change as needed across code base
#
#   export GW_ACCOUNT_HOME=/home/pi
#   export GW_HOME=$GW_ACCOUNT_HOME/ctdgtwy
#   if [ ! -d $GW_HOME/logs ] 
#     then
#       sudo mkdir $GW_HOME/logs 
#   fi
#   sudo echo "$(date) booting..." >> $GW_HOME/logs/booting.log 
#   $(cd $GW_HOME/ ; sh deploy_and_start_ctd_on_boot.sh) &
#
# and don't forget to make autorun.sh executable (sudo chmod 755 autorun.sh)
