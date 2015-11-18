using System.Collections.Generic;
using System.Configuration;


namespace WorkerHost
{
    public class AppConfiguration
        {
            public string DeviceEHConnectionString;
            public string DeviceEHName;


            public string NotificationService;
            public string EmailServiceUserName;
            public string EmailServicePassword;

            public string SmtpHost;
            public bool   SmtpEnableSSL;

            public string MessageFromAddress;
            public string MessageFromName;
            public string MessageSubject;
            public string ConsumerGroupPrefix;
            
            public IList<string> SendToList;
        }

    public class SendToConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsRequired = true, IsDefaultCollection = true)]
        public SendToConfigInstanceCollection Instances
        {
            get { return (SendToConfigInstanceCollection)this[""]; }
            set { this[""] = value; }
        }
    }

    public class SendToConfigInstanceCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new SendToConfigInstanceElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SendToConfigInstanceElement)element).Address;
        }
    }

    public class SendToConfigInstanceElement : ConfigurationElement
    {
        [ConfigurationProperty("address", IsKey = true, IsRequired = true)]
        public string Address
        {
            get
            {
                return (string)base["address"];
            }
        }
    }

    internal class SendFromConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("address", DefaultValue = "address", IsRequired = true)]
        public string Address
        {
            get
            {
                return (string)this["address"];
            }
            set
            {
                this["address"] = value;
            }
        }

        [ConfigurationProperty("displayName", DefaultValue = "displayName", IsRequired = true)]
        public string DisplayName
        {
            get
            {
                return (string)this["displayName"];
            }
            set
            {
                this["displayName"] = value;
            }
        }

        [ConfigurationProperty("subject", DefaultValue = "subject", IsRequired = true)]
        public string Subject
        {
            get
            {
                return (string)this["subject"];
            }
            set
            {
                this["subject"] = value;
            }
        }
    }

    public static class ConfigurationLoader
    {
        public static IList<string> GetSendToList()
        {
            var addresses = new List<string>();

            SendToConfigSection config = ConfigurationManager.GetSection("sendToList") as SendToConfigSection;

            if (config != null)
            {
                foreach (SendToConfigInstanceElement e in config.Instances)
                {
                    addresses.Add(e.Address);
                }
            }

            return addresses;
        }

        public static AppConfiguration GetConfig()
        {
            SendFromConfigSection sendFromSection = (ConfigurationManager.GetSection("sendFrom") as SendFromConfigSection);

            if (sendFromSection == null)
            {
                return null;
            }

            AppConfiguration config = new AppConfiguration
            {
                DeviceEHConnectionString =
                    ConfigurationManager.AppSettings.Get("Microsoft.ServiceBus.EventHubConnectionString"),
                DeviceEHName = ConfigurationManager.AppSettings.Get("Microsoft.ServiceBus.EventHubToMonitor"),
                NotificationService = ConfigurationManager.AppSettings.Get("NotificationService"),
                EmailServiceUserName = ConfigurationManager.AppSettings.Get("SenderUserName"),
                EmailServicePassword = ConfigurationManager.AppSettings.Get("SenderPassword"),
                SmtpHost = ConfigurationManager.AppSettings.Get("SmtpHost"),
                SmtpEnableSSL = ConfigurationManager.AppSettings.Get("SmtpEnableSSL").ToLowerInvariant() == "true",

                MessageFromAddress = sendFromSection.Address,
                MessageFromName = sendFromSection.DisplayName,
                MessageSubject = sendFromSection.Subject,

                SendToList = ConfigurationLoader.GetSendToList(),
                ConsumerGroupPrefix = ConfigurationManager.AppSettings.Get("ConsumerGroupPrefix"),
            };

            return config;
        }
    }

    
}
