//#define DEBUG_LOG
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;
using SendGrid;
using Twilio;

namespace WorkerHost
{
    public class WorkerHost : RoleEntryPoint
    {
        private static EventHubReader          _EventHubReader;
        private static Timer                   _Timer;

        private static object                  _SenderLock = new object();
        private static Web                     _SendGridTransportWeb;
        private static SmtpClient              _SmtpClient;
        private static TwilioRestClient        _TwilioRestClient;

        private static MailAddress             _FromAddress;
        private static MailAddress[]           _ToAddress;

        private static AppConfiguration        _Config;

        private static NotificationServiceType _NotificationService;
        enum NotificationServiceType
        {
            Smtp = 1,
            SendGridWeb = 2,
            Twilio = 3,
            TwilioCall = 4
        }

        static void Main()
        {
            StartHost("L0cal");
        }

        public override void Run()
        {
            StartHost("R0le");
        }

        private static void StartHost(string consumerGroupSuffix)
        {
            Trace.WriteLine("Starting Worker...");
#if DEBUG_LOG
            RoleEnvironment.TraceSource.TraceInformation("Starting Worker...");
#endif

            _Config = ConfigurationLoader.GetConfig();
            if (_Config == null)
            {
                return;
            }
            _Config.ConsumerGroupPrefix += consumerGroupSuffix;

            _NotificationService =
                  (NotificationServiceType)Enum.Parse(typeof(NotificationServiceType), _Config.NotificationService);

            var credentials = new NetworkCredential(_Config.EmailServiceUserName, _Config.EmailServicePassword);

            PrepareMailAddressInstances();

            switch (_NotificationService)
            {
                case NotificationServiceType.Smtp:
                    {
                        _SmtpClient = new SmtpClient
                        {
                            Port = _Config.SmtpEnableSSL ? 587 : 25,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            EnableSsl = false,
                            Host = _Config.SmtpHost,
                            Credentials = credentials
                        };
                    }
                    break;
                case NotificationServiceType.SendGridWeb:
                    {
                        _SendGridTransportWeb = new Web(credentials);
                    }
                    break;
                case NotificationServiceType.Twilio:
                case NotificationServiceType.TwilioCall:
                    {
                        string ACCOUNT_SID = _Config.EmailServiceUserName;
                        string AUTH_TOKEN = _Config.EmailServicePassword;

                        _TwilioRestClient = new TwilioRestClient(ACCOUNT_SID, AUTH_TOKEN);
                    }
                    break;
            }

            _EventHubReader = new EventHubReader(_Config.ConsumerGroupPrefix, OnMessage);

            Process();
        }

        private static void PrepareMailAddressInstances()
        {
            try
            {
                _FromAddress = new MailAddress(_Config.MessageFromAddress, _Config.MessageFromName);
            }
            catch (Exception)
            {
                //incorrect mail address in config, maybe a phone number
            }

            IList<MailAddress> mailAddressList = new List<MailAddress>();

            foreach (var sendTo in _Config.SendToList)
            {
                try
                {
                    mailAddressList.Add(new MailAddress(sendTo));
                }
                catch (Exception)
                {
                    //incorrect mail address in config, maybe a phone number
                }
            }

            _ToAddress = mailAddressList.ToArray();
        }

        public static void Process()
        {
            _EventHubReader.Run(_Config.DeviceEHConnectionString, _Config.DeviceEHName, string.Empty);
            _EventHubReader.FailureEvent.WaitOne();
        }

        private static void OnMessage(string serializedData)
        {
            lock (_SenderLock)
            {
                try
                {
                    if (!_Config.SendToList.Any()) return;

                    string messageBody = "Message Received: \n" + serializedData;

                    if (_SmtpClient != null)
                    {
                        foreach (var mailTo in _ToAddress)
                        {
                            try
                            {
                                MailMessage myMessage = new MailMessage(_FromAddress, mailTo)
                                {
                                    Subject = _Config.MessageSubject,
                                    Body = messageBody
                                };

                                _SmtpClient.Send(myMessage);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

                    if (_SendGridTransportWeb != null)
                    {
                        SendGridMessage myMessage = new SendGridMessage(
                            _FromAddress,
                            _ToAddress,
                            _Config.MessageSubject,
                            string.Empty,
                            messageBody
                            );

                        _SendGridTransportWeb.DeliverAsync(myMessage).Wait();
                    }

                    if (_TwilioRestClient != null)
                    {

                        switch (_NotificationService)
                        {
                            case NotificationServiceType.Twilio:
                                {
                                    foreach (var smsTo in _Config.SendToList)
                                    {
                                        try
                                        {
                                            _TwilioRestClient.SendMessage(_Config.MessageFromAddress, smsTo,
                                                messageBody);
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                }
                                break;
                            case NotificationServiceType.TwilioCall:
                                {
                                    string callUrl = "http://twimlets.com/message?Message%5B0%5D=" + WebUtility.UrlEncode(_Config.MessageSubject);

                                    foreach (var callTo in _Config.SendToList)
                                    {
                                        try
                                        {
                                            CallOptions options = new CallOptions
                                            {
                                                From = _Config.MessageFromAddress,
                                                To = callTo,
                                                Url = callUrl
                                            };

                                            Call call = _TwilioRestClient.InitiateOutboundCall(options);
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
                catch (Exception)
                {
                    Trace.WriteLine("Exception on mail sending...");
                }
            }
        }
    }
}
