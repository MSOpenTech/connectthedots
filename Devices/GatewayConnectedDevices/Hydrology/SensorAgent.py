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
 Code to read several Hydrology sensors, then augment and format as JSON to send via socket connection to a gateway.
 Example of sending hydrology data to Microsoft Azure and analyzing with Azure Stream Analytics or Azure Machine Learning.
 Real time output viewable at http://connectthedots.msopentech.com .
'''
import sys
import socket
import time
import datetime
import re

from DO2Sensor import DO2Sensor
from ECSensor import ECSensor
from MoistureSensor import MoistureSensor

Org =       "MSOpenTech"
Disp =      "Hydrology Sensors"                     # will be the label for the curve on the chart
GUID =      "nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn"  # ensures all the data from this sensor appears on the same chart.  You can
                                                    # use the Tools/Create GUID in Visual Studio to create
Locn =      "here"
UnitsMap =  {'Moisture':'unitless', 'Conductivity':'uS/cm', 'TDS':'ppm', 'Salinity':'unitless', 'SpecificGravity':'unitless', 'DissolvedOxygen':'ml/l' }

Vendor =    0xfffe                                   # Vendor ID for our custom device
Product =   0xfffe                                   # Product ID for our custom device
HOST =      '127.0.0.1'
PORT =      5001

CONNECT_RETRY_INTERVAL = 2
EXCEPTION_THRESHOLD = 3
SEND_INTERVAL = 5

class SensorAgent:

    s = None
    do2Sensor = None
    ecSensor = None
    moistureSensor = None

    def __init__(self) :
        self.do2Sensor = DO2Sensor()
        self.ecSensor = ECSensor()
        self.moistureSensor = MoistureSensor()

    def connectToServer(self) :
        self.s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        print("Socket created.")

        while True:
            try:
                self.s.connect((HOST, PORT))
                break
            except socket.error as msg:
                print("Socket connection failed. Error Code : " + str(msg[0]) + " Message " + msg[1])
                time.sleep(CONNECT_RETRY_INTERVAL)

        print ("Socket connection succeeded.")

    def close(self) :
        self.s.close()

    def sendMessage(self, measureName, value) :
        timeStr = datetime.datetime.utcnow().isoformat()
        JSONdB = "{\"value\":" + value + ",\"guid\":\"" + GUID + "\",\"organization\":\"" + Org + "\",\"displayname\":\"" + Disp + "\",\"unitofmeasure\":\"" + UnitsMap[measureName] + "\",\"measurename\":\"" + measureName + "\",\"location\":\"" + Locn + "\",\"timecreated\":\"" + timeStr + "\"}"
        if self.s != None :
            self.s.send("<" + JSONdB + ">")         # sends to gateway over socket interface
        print(JSONdB)                               # print only for debugging purposes

    def processSensorData(self) :
        DO2Sample = self.do2Sensor.GetDataSample()
        DO2Parsed = re.findall(r"([0-9]+\.[0-9]+)",DO2Sample)
        if DO2Parsed:
            self.sendMessage('Dissolved Oxygen', DO2Parsed[1])

        ECSample = self.ecSensor.query("R")
        print(ECSample)
        ECParsed = re.match("Command success:([0-9]+\.*[0-9]*),([0-9]+\.*[0-9]*),([0-9]+\.*[0-9]*),([0-9]+\.*[0-9]*)",ECSample)
        if ECParsed :
            self.sendMessage('Conductivity',    ECParsed.group(1))
            self.sendMessage('TDS',             ECParsed.group(2))
            self.sendMessage('Salinity',        ECParsed.group(3))
            self.sendMessage('Specific Gravity',ECParsed.group(4))

        MoistureSample = self.moistureSensor.GetDataSample()
        self.sendMessage('Moisture', str(MoistureSample))

def main() :
    agent = SensorAgent()

    while True:
        exceptions_count = 0
        agent.connectToServer()

        while True:
            try:
                start = time.time()
                agent.processSensorData()
                end = time.time()
                if end - start < SEND_INTERVAL :
                    time.sleep(SEND_INTERVAL - (end - start))

            except Exception as msg:
                exceptions_count += 1
                print(msg[0])
                # if we get too many exceptions, we assume the server is dead
                # we will ignore the casual exception
                if exceptions_count > EXCEPTION_THRESHOLD:
                    break 
                else:
                    continue

        # will never get here, unless server dies         
        try: 
            agent.close()
        except KeyboardInterrupt: 
            print("Continuous polling stopped")
        except Exception as msg:
            # eat all exception and go back to connect loop 
            print(msg[0])

if __name__ == '__main__':
    main()
