//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Diagnostics;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using System.Collections;

namespace ConnectTheDotsWebSite
{
    public class DevicesControlServiceController : ApiController
    {
        private TopicClient devicesTopicClient;
        private static readonly List<string> deviceIdsList = new List<string>() { "all", "D1", "D2", "D3", "D4" };

        DevicesControlServiceController()
        {
            var devicesTopicConnectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionStringDevicesService");
            var devicesTopicName = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.TopicDevices");
            var messagingFactory = MessagingFactory.CreateFromConnectionString(devicesTopicConnectionString);
            devicesTopicClient = messagingFactory.CreateTopicClient(devicesTopicName);
        }

        [HttpPost]
        [ActionName("WriteTextOnScreen")]
        // POST api/DevicesControlService/WriteTextOnScreen?deviceId=[Id]
        // text=[text]&br=[backgroundRed]&bg=[backgroundGreen]&bb=[backgroundBlue]
        public void WriteTextOnScreen(string deviceId, TextScreenRequest request)
        {
            if (!deviceIdsList.Exists((d) => String.Equals(d, deviceId)))
                throw new HttpResponseException(HttpStatusCode.NotFound);

            request.text = request.text.Replace('"','/');

            Trace.TraceInformation("Write {0} On The Screen  of {1} device", deviceId, request.text);

            var command = new BrokeredMessage(String.Format("{{\"command\":\"writetextonscreen\",\"text\":\"{0}\",\"rgb\":\"{1},{2},{3}\"}}",
                    request.text, request.br, request.bg, request.bb));
            command.Properties["deviceId"] = deviceId;

            devicesTopicClient.Send(command);
        }

        [HttpPost]
        [ActionName("ChangeStateOfLight")]
        // POST api/DevicesControlService/ChangeStateOfLight?deviceId=[Id]
        // lightState=[on|off]
        public void ChangeStateOfLight(string deviceId, LightRequest request)
        {
            if (!deviceIdsList.Exists((d) => String.Equals(d, deviceId))
                || (request.state != "on" && request.state != "off"))
                throw new HttpResponseException(HttpStatusCode.NotFound);

            Trace.TraceInformation("Turn {0} The Light for device {1}", request.state, deviceId);

            var command = new BrokeredMessage(String.Format("{{\"command\":\"changestateoflight\",\"state\":\"{0}\"}}", request.state));
            command.Properties["deviceId"] = deviceId;

            devicesTopicClient.Send(command);
        }

        [HttpPost]
        [ActionName("Beep")]
        // POST api/DevicesControlService/Beep?deviceId=[Id]
        // freq=[freq]&delay=[delay]
        public void Beep(string deviceId, BeepRequest request)
        {
            if (!deviceIdsList.Exists((d) => String.Equals(d, deviceId)))
                throw new HttpResponseException(HttpStatusCode.NotFound);

            Trace.TraceInformation("Beep on {0} with delay {1} and frequency {2}", deviceId, request.delay, request.freq);

            var command = new BrokeredMessage(String.Format("{{\"command\":\"beep\",\"freq\":{0},\"delay\":{1}}}", request.delay, request.freq));
            command.Properties["deviceId"] = deviceId;

            devicesTopicClient.Send(command);
        }
    }

    public class TextScreenRequest
    {
        public string text { get; set; }
        public int br { get; set; } 
        public int bg { get; set; }
        public int bb { get; set; }
    }

    public class BeepRequest
    {
        public long freq { get; set; }
        public long delay { get; set; }
    }

    public class LightRequest
    {
        public string state { get; set; }
    }
}