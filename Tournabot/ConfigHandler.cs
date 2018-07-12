using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Tournabot
{
    public class ConfigHandler
    {
        private Config conf;
        private string configPath;

        struct Config
        {
            public string token;
            public string sql;
            public string ToornamentApiKey;
            public string ToornamentId;
            public string ToornamentSecret;
        }

        public ConfigHandler()
        {
            conf = new Config()
            {
                token = "",
                sql = "",
                ToornamentApiKey = "",
                ToornamentId = "",
                ToornamentSecret = ""
            };
        }

        public async Task PopulateConfig()
        {
            configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json").Replace(@"\", @"\\");
            Console.WriteLine(configPath);

            if (!File.Exists(configPath))
            {
                /*using (var f = File.Create(configPath))
                {
                    DirectoryInfo dInfo = new DirectoryInfo(configPath);
                    DirectorySecurity dSecurity = dInfo.GetAccessControl();
                    dSecurity.AddAccessRule(new FileSystemAccessRule("everyone", FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                    dInfo.SetAccessControl(dSecurity);
                }*/

                using (StreamWriter sw = File.AppendText(configPath))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(conf));
                }

                Console.WriteLine("WARNING! New Config initialized! Need to fill in values before running commands!");
                throw new Exception("NO CONFIG AVAILABLE! Go to executable path and fill out newly created file!");
            }

            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }

            await Task.CompletedTask;
        }

        public string GetToken()
        {
            return conf.token;
        }

        public string GetSql()
        {
            return conf.sql;
        }

        public string GetToornamentApiKey()
        {
            return conf.ToornamentApiKey;
        }

        public string GetToornamentId()
        {
            return conf.ToornamentId;
        }

        public string GetToornamentSecret()
        {
            return conf.ToornamentSecret;
        }
    }
}
