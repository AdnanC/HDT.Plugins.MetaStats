using HDT.Plugins.MetaStats.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HDT.Plugins.MetaStats
{
    public class MetaConfig
    {
        private static string configDirectory = Path.Combine(Hearthstone_Deck_Tracker.Config.AppDataPath, "MetaStats");
        private static string configPath = Path.Combine(configDirectory, "metaConfig.xml");

        public string currentVersion { get; set; }
        public DateTime lastCheck { get; set; }
        public DateTime lastUpload { get; set; }
        public string userKey { get; set; }

        public MetaConfig()
        {

        }

        public MetaConfig(string v, DateTime c, DateTime u, string user = "")
        {
            this.currentVersion = v;
            this.lastCheck = c;
            this.lastUpload = u;
            this.userKey = user;
        }

        public static MetaConfig Load()
        {
            var userAddr =
                            (
                            from nic in NetworkInterface.GetAllNetworkInterfaces()
                            where nic.OperationalStatus == OperationalStatus.Up
                            select nic.GetPhysicalAddress().ToString()
                            ).FirstOrDefault();

            userAddr = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(userAddr));

            try
            {
                if (File.Exists(configPath))
                {
                    var serializer = new XmlSerializer(typeof(MetaConfig));

                    using (var reader = new StreamReader(configPath))
                    {
                        MetaConfig temp = (MetaConfig)serializer.Deserialize(reader);
                        if (temp.userKey == null)
                        {
                            temp.userKey = userAddr;
                        }
                        return temp;
                    }
                }
                else
                {
                    return new MetaConfig("1", DateTime.Now, DateTime.Now, userAddr);
                }
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
                return new MetaConfig("1", DateTime.Now, DateTime.Now, userAddr);
                //return null;
            }
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(configDirectory))
                    Directory.CreateDirectory(configDirectory);

                var serializer = new XmlSerializer(typeof(MetaConfig));
                using (var writer = new StreamWriter(configPath))
                    serializer.Serialize(writer, this);
            }
            catch (Exception ex)
            {
                MetaLog.Error(ex);
            }
        }
    }
}
