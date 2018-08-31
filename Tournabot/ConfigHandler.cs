using Discord.Rest;
using Newtonsoft.Json;
using System;
using System.IO;
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
            public string signUpChannel;
            public string signUpMessage;
            public string checkInMessage;
            public string regionMessage;
        }

        public ConfigHandler()
        {
            conf = new Config()
            {
                token = "",
                sql = "",
                signUpChannel = "",
                signUpMessage = "",
                checkInMessage = "",
                regionMessage = "",
            };
        }

        public async Task PopulateConfig()
        {
            configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json").Replace(@"\", @"\\");

            if (!File.Exists(configPath))
            {
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

        public ulong GetSignUpChannel()
        {
            return ulong.Parse(conf.signUpChannel);
        }

        public ulong GetSignUpMessage()
        {
            return ulong.Parse(conf.signUpMessage);
        }

        public void SaveSignUpMessage(RestUserMessage message)
        {
            conf.signUpMessage = message.Id.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetCheckInMessage()
        {
            return ulong.Parse(conf.checkInMessage);
        }

        public void SaveCheckInMessage(RestUserMessage message)
        {
            conf.checkInMessage = message.Id.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetRegionMessage()
        {
            return ulong.Parse(conf.regionMessage);
        }

        public void SaveRegionMessage(RestUserMessage message)
        {
            conf.regionMessage = message.Id.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }
    }
}
