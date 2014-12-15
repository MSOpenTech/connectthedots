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
echo "Running gateway"
sudo -u pi /usr/bin/mono /home/pi/RaspberryPiGateway/RaspberryPiGateway.exe -forever -deviceid C8CA5B13-A550-4FF0-B823-46D8A2640880 -devicename "R-Pi SS dev device" -address amqps://D2:WxEVuhSvRzm2bX7TZt9QjOZKhsG5yr69Yl8PcHKPcf4%3D@myiot-ns.servicebus.windows.net -target ehdevices &

# Add the below line to /etc/rc.local
#   /home/pi/RaspberryPiGateway/autorun.sh &
# and don't forget to make autorun.sh executable (sudo chmod 755 autorun)
