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
 Code to read a moisture sensor attached to a RedBear BLE Nano, then augment and format as JSON to send via socket connection to a gateway.
 Example of sending hydrology data to Microsoft Azure and analyzing with Azure Stream Analytics or Azure Machine Learning.
 
'''
import sys
import socket
import time
import datetime
import re
import csv
import sys

Debug = False

IronPythonPlatform = 'cli'

if sys.platform != IronPythonPlatform:
    from BLEMoistureSensor import BLEMoistureSensor

CONNECT_RETRY_INTERVAL = 2
EXCEPTION_THRESHOLD = 3
SEND_INTERVAL = 5

s = None
deviceConfig = {}
sensorAgentConfig = None

def processSensorData(macAddress, value) :
    global s
    global deviceConfig
    global sensorAgentConfig
    timeStr = datetime.datetime.utcnow().isoformat()

    macAddressRecognized = False

    # replace last group of digits with mac address of BLE sensor board
    deviceID = sensorAgentConfig["GUID"]
    deviceID = deviceID[:24] + macAddress
    JSONString = "{"
    JSONString += "\"value\": %s" % value
    JSONString += ",\"guid\":\"" + deviceID

    macAddressKey = macAddress
    displayName = ""
    if macAddress in deviceConfig:
        macAddressRecognized = True
        displayName = deviceConfig[macAddressKey]["DisplayName"]
    elif '*' in deviceConfig:
        macAddressKey = '*'
        macAddressRecognized = True
        displayName = macAddress

    if macAddressRecognized == True:
        JSONString += "\",\"organization\":\"" + deviceConfig[macAddressKey]["Organization"]
        JSONString += "\",\"displayname\":\"" + displayName
        JSONString += "\",\"unitofmeasure\":\"" + deviceConfig[macAddressKey]["UnitsOfMeasure"]
        JSONString += "\",\"measurename\":\"" + deviceConfig[macAddressKey]["MeasureName"]
        JSONString += "\",\"location\":\"" + deviceConfig[macAddressKey]["Location"]
        JSONString += "\",\"timecreated\":\"" + timeStr + "\""
        JSONString += "}"

        if Debug == True:
            print "JSONString=", JSONString
        if s != None :
            # send JSON string to gateway over socket interface
            s.send("<" + JSONString + ">")

def main() :
    global s
    global deviceConfig
    global sensorAgentConfig

    # parse SensorAgent configuration CSV file
    try:
        with open('SensorAgentConfig.csv') as sensorAgentConfigFile:
            sensorAgentConfigSource = csv.DictReader(sensorAgentConfigFile) 
            for row in sensorAgentConfigSource :
                sensorAgentConfig = row
                # we only care about first row in config file
                break;
    except:
        print "Error reading config file. Please correct before continuing."
        sys.exit()


    # parse device configuration (BLE device) CSV file
    try:
        with open('DeviceConfig.csv') as deviceConfigFile:
            deviceConfigSource = csv.DictReader(deviceConfigFile) 
            for row in deviceConfigSource:
                deviceConfig[row["MACAddress"]] = row
    except:
        print "Error reading config file. Please correct before continuing."
        sys.exit()

    try:
        # setup moisture sensor
        if sys.platform != 'cli':
            moistureSensor = BLEMoistureSensor()
            moistureSensor.setSensorDataAvailableEvent(processSensorData)

        # setup server socket
        if Debug == False :
            s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            print "Socket created."
            while True:
                try:
                    s.connect((sensorAgentConfig["Host"], int(sensorAgentConfig["Port"])))
                    break
                except socket.error as msg:
                    print "Socket connection failed. Error Code : " + str(msg[0]) + " Message " + msg[1]
                    time.sleep(CONNECT_RETRY_INTERVAL)
                    print "Socket connection succeeded."

        # this will listen forever for advertising events and call
        # processSensorData() when data arrives
        if sys.platform != IronPythonPlatform:
            moistureSensor.Listen()

    except KeyboardInterrupt: 
        print "Continuous polling stopped"

if __name__ == '__main__':
    main()
