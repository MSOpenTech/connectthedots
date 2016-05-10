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
 Code to read data from a Decagon 10HS moisture sensor connected to a RedBear BLE Nano micro controller, then augment and format as JSON to send via 
 socket connection to a gateway. Example of sending moisture to Microsoft Azure and analyzing with Azure Stream Analytics or Azure Machine Learning.
 Real time output viewable at http://connectthedots.msopentech.com .
'''

# code based on https://github.com/switchdoclabs/iBeacon-Scanner-

DEBUG = False

import os
import sys
import struct
import bluetooth._bluetooth as bluez

LE_META_EVENT = 0x3e
LE_PUBLIC_ADDRESS=0x00
LE_RANDOM_ADDRESS=0x01
LE_SET_SCAN_PARAMETERS_CP_SIZE=7
OGF_LE_CTL=0x08
OCF_LE_SET_SCAN_PARAMETERS=0x000B
OCF_LE_SET_SCAN_ENABLE=0x000C
OCF_LE_CREATE_CONN=0x000D

LE_ROLE_MASTER = 0x00
LE_ROLE_SLAVE = 0x01

# these are actually sub-events of LE_META_EVENT
EVT_LE_CONN_COMPLETE=0x01
EVT_LE_ADVERTISING_REPORT=0x02
EVT_LE_CONN_UPDATE_COMPLETE=0x03
EVT_LE_READ_REMOTE_USED_FEATURES_COMPLETE=0x04

# Advertisement event types
ADV_IND=0x00
ADV_DIRECT_IND=0x01
ADV_SCAN_IND=0x02
ADV_NONCONN_IND=0x03
ADV_SCAN_RSP=0x04

def eventHandler(macAddress, value):
    f(macAddress,value)

class BLEMoistureSensor:

    sock = None
    callback = None
    dev_id = 0
    
    def __init__(self) :
        try:
            self.sock = bluez.hci_open_dev(self.dev_id)
            old_filter = self.sock.getsockopt( bluez.SOL_HCI, bluez.HCI_FILTER, 14)
            enable = 1
            cmd_pkt = struct.pack("<BB", enable, 0x00)
            bluez.hci_send_cmd(self.sock, OGF_LE_CTL, OCF_LE_SET_SCAN_ENABLE, cmd_pkt)
            
        except:
            print "error accessing blue tooth device..."
            sys.exit(1)
    
    def printpacket(self, pkt):
        print "in printpacket"
        for c in pkt:
            sys.stdout.write("%02x " % struct.unpack("B",c)[0])

    def packed_bdaddr_to_string(self, bdaddr_packed):
        return ''.join('%02x'%i for i in struct.unpack("<BBBBBB", bdaddr_packed[::-1]))

    # func( macAddress, value )
    def setSensorDataAvailableEvent(self, func):
        self.callback = func
        
    def Listen(self):
        try:
            old_filter = self.sock.getsockopt( bluez.SOL_HCI, bluez.HCI_FILTER, 14)

            # perform a device inquiry on blue tooth device #0
            # The inquiry should last 8 * 1.28 = 10.24 seconds
            # before the inquiry is performed, bluez should flush its cache of
            # previously discovered devices
            flt = bluez.hci_filter_new()
            bluez.hci_filter_all_events(flt)
            bluez.hci_filter_set_ptype(flt, bluez.HCI_EVENT_PKT)
            self.sock.setsockopt( bluez.SOL_HCI, bluez.HCI_FILTER, flt )
            while True:
                pkt = self.sock.recv(255)
                ptype, event, plen = struct.unpack("BBB", pkt[:3])

                if event == bluez.EVT_INQUIRY_RESULT_WITH_RSSI:
                    i =0
                elif event == bluez.EVT_NUM_COMP_PKTS:
                        i =0 
                elif event == bluez.EVT_DISCONN_COMPLETE:
                        i =0 
                elif event == LE_META_EVENT:
                    subevent, = struct.unpack("B", pkt[3])
                    pkt = pkt[4:]
                    if subevent == EVT_LE_CONN_COMPLETE:
                        le_handle_connection_complete(pkt)
                    elif subevent == EVT_LE_ADVERTISING_REPORT:
                        #print "advertising report"
                        num_reports = struct.unpack("B", pkt[0])[0]
                        report_pkt_offset = 0
                        for i in range(0, num_reports):
                            if (DEBUG == True):
                                print "-------------"
                                print "\t", "full packet: ", self.printpacket(pkt)
                                print "\t", "MAC address: ", self.packed_bdaddr_to_string(pkt[report_pkt_offset + 3:report_pkt_offset + 9])
                            # build the return string
                            id = pkt[report_pkt_offset +12: report_pkt_offset +26] 
                            if (DEBUG == True):
                                print "\t", "id: ", id
                            if (id == 'MSOT_BLE_Demo:'):
                                # MAC address
                                macAddress = self.packed_bdaddr_to_string(pkt[report_pkt_offset + 3:report_pkt_offset + 9])
                                # string representation of Water Volume Content (unit-less) floating point value
                                value = pkt[report_pkt_offset + 26: report_pkt_offset + 36] 
                                if (DEBUG == True):
                                    print "\t", "address=", macAddress, " value=", value
                                if( self.callback != None ):
                                    print "calling event handler"
                                    self.callback( macAddress, value )
        except:
            self.sock.setsockopt( bluez.SOL_HCI, bluez.HCI_FILTER, old_filter )
            print "error in BLE Listen loop"
            sys.exit(1)
