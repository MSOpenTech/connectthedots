'''
 Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.

 The MIT License (MIT)

 Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 
 The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 
 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 THE SOFTWARE.
  
 -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-= 
 Code to read data from an Atlas Scientific Dissolved Oxygen Sensor Board.
'''

import serial
import re

class DO2Sensor(object):
    default_device = '/dev/ttyAMA0'
    port = serial.Serial()

    def __init__(self, device = default_device):
        # DO2 sensor is on UART because we have the legacy version of the DO2 board
        self.port = serial.Serial( device,38400,timeout=1, bytesize=8, parity='N', stopbits=1, xonxoff=0, rtscts=0)

    def GetDataSample(self) :
        # get data command for DO2 sensor board
        self.port.write('R\r')
        line = self.port.readline()
        # occasionally no data is returned
        while len(line) < 3:
            self.port.write('R\r')
            line = self.port.readline()
        return line;

def main() :
    device = DO2Sensor()
    try:
        while True:
            DO2Sample = device.GetDataSample()
            DO2Parsed = re.findall(r"([0-9]+\.[0-9]+)",DO2Sample)
            if DO2Parsed:
                print "Dissolved O2 = {0} % ({1} mg/L)".format( DO2Parsed[0], DO2Parsed[1] )
            else:
                print "failed to parse:" + DO2Sample
    except KeyboardInterrupt: # catches the ctrl-c command, which breaks the loop above
        print("Continuous polling stopped")

if __name__ == '__main__':
    main()
