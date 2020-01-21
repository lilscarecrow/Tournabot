using Discord;
using Discord.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            public string bracketInfoChannel;
            public string signUpChannel;
            public string regionChannel;
            public string signUpMessage;
            public string checkInMessage;
            public string waitListMessage;
            public string regionMessage;
            public string directorRole;
            public string finalsDirectorRole;
            public string champion;
            public string guild;
            public string scrimEastRole;
            public string scrimEastActiveRole;
            public string scrimEastSignUpChannel;
            public string scrimEastMessage;
            public string scrimWestRole;
            public string scrimWestActiveRole;
            public string scrimWestSignUpChannel;
            public string scrimWestMessage;
            public string scrimEURole;
            public string scrimEUActiveRole;
            public string scrimEUSignUpChannel;
            public string scrimEUMessage;
            public string scrimSARole;
            public string scrimSAActiveRole;
            public string scrimSASignUpChannel;
            public string scrimSAMessage;
            public string scrimSPRole;
            public string scrimSPActiveRole;
            public string scrimSPSignUpChannel;
            public string scrimSPMessage;
            public string scrimAURole;
            public string scrimAUActiveRole;
            public string scrimAUSignUpChannel;
            public string scrimAUMessage;
            public string scrimAdminRole;
            public string scrimAdminChannel;
            public string dashboardMessage;
            public string scrimAdminLogsChannel;
            public string scrimChannel;
            public string scrimMessage;
            public string scrimSize;
        }

        public ConfigHandler()
        {
            conf = new Config()
            {
                token = "",
                sql = "",
                bracketInfoChannel = "",
                signUpChannel = "",
                regionChannel = "",
                signUpMessage = "",
                checkInMessage = "",
                waitListMessage = "",
                regionMessage = "",
                directorRole = "",
                finalsDirectorRole = "",
                champion = "",
                guild = "",
                scrimEastRole = "",
                scrimEastActiveRole = "",
                scrimEastSignUpChannel = "",
                scrimEastMessage = "",
                scrimWestRole = "",
                scrimWestActiveRole = "",
                scrimWestSignUpChannel = "",
                scrimWestMessage = "",
                scrimEURole = "",
                scrimEUActiveRole = "",
                scrimEUSignUpChannel = "",
                scrimEUMessage = "",
                scrimSARole = "",
                scrimSAActiveRole = "",
                scrimSASignUpChannel = "",
                scrimSAMessage = "",
                scrimSPRole = "",
                scrimSPActiveRole = "",
                scrimSPSignUpChannel = "",
                scrimSPMessage = "",
                scrimAURole = "",
                scrimAUActiveRole = "",
                scrimAUSignUpChannel = "",
                scrimAUMessage = "",
                scrimAdminRole = "",
                scrimAdminChannel = "",
                dashboardMessage = "",
                scrimAdminLogsChannel = "",
                scrimChannel = "",
                scrimMessage = "",
                scrimSize = ""
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

        public ulong GetBracketInfoChannel()
        {
            return ulong.Parse(conf.bracketInfoChannel);
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

        public ulong GetWaitListMessage()
        {
            return ulong.Parse(conf.waitListMessage);
        }

        public void SaveWaitListMessage(RestUserMessage message)
        {
            conf.waitListMessage = message.Id.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetRegionChannel()
        {
            return ulong.Parse(conf.regionChannel);
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

        public ulong GetDirectorRole()
        {
            return ulong.Parse(conf.directorRole);
        }

        public ulong GetFinalsDirectorRole()
        {
            return ulong.Parse(conf.finalsDirectorRole);
        }

        public ulong GetChampionRole()
        {
            return ulong.Parse(conf.champion);
        }

        public ulong GetGuild()
        {
            return ulong.Parse(conf.guild);
        }

        public ulong GetEastScrimRole()
        {
            return ulong.Parse(conf.scrimEastRole);
        }

        public void SetEastScrimRole(ulong role)
        {
            conf.scrimEastRole = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetEastScrimActiveRole()
        {
            return ulong.Parse(conf.scrimEastActiveRole);
        }

        public void SetEastScrimActiveRole(ulong role)
        {
            conf.scrimEastActiveRole = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetEastScrimChannel()
        {
            return ulong.Parse(conf.scrimEastSignUpChannel);
        }

        public ulong GetWestScrimRole()
        {
            return ulong.Parse(conf.scrimWestRole);
        }

        public ulong GetEastScrimMessage()
        {
            return ulong.Parse(conf.scrimEastMessage);
        }

        public void SetEastScrimMessage(ulong role)
        {
            conf.scrimEastMessage = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public void SetWestScrimRole(ulong role)
        {
            conf.scrimWestRole = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetWestScrimActiveRole()
        {
            return ulong.Parse(conf.scrimWestActiveRole);
        }

        public void SetWestScrimActiveRole(ulong role)
        {
            conf.scrimWestActiveRole = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetWestScrimChannel()
        {
            return ulong.Parse(conf.scrimWestSignUpChannel);
        }

        public ulong GetWestScrimMessage()
        {
            return ulong.Parse(conf.scrimWestMessage);
        }

        public void SetWestScrimMessage(ulong role)
        {
            conf.scrimWestMessage = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetEUScrimRole()
        {
            return ulong.Parse(conf.scrimEURole);
        }

        public void SetEUScrimRole(ulong role)
        {
            conf.scrimEURole = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetEUScrimActiveRole()
        {
            return ulong.Parse(conf.scrimEUActiveRole);
        }

        public void SetEUScrimActiveRole(ulong role)
        {
            conf.scrimEUActiveRole = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetEUScrimChannel()
        {
            return ulong.Parse(conf.scrimEUSignUpChannel);
        }

        public ulong GetEUScrimMessage()
        {
            return ulong.Parse(conf.scrimEUMessage);
        }

        public void SetEUScrimMessage(ulong role)
        {
            conf.scrimEUMessage = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetSAScrimRole()
        {
            return ulong.Parse(conf.scrimSARole);
        }

        public void SetSAScrimRole(ulong role)
        {
            conf.scrimSARole = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetSAScrimActiveRole()
        {
            return ulong.Parse(conf.scrimSAActiveRole);
        }

        public void SetSAScrimActiveRole(ulong role)
        {
            conf.scrimSAActiveRole = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetSAScrimChannel()
        {
            return ulong.Parse(conf.scrimSASignUpChannel);
        }

        public ulong GetSAScrimMessage()
        {
            return ulong.Parse(conf.scrimSAMessage);
        }

        public void SetSAScrimMessage(ulong role)
        {
            conf.scrimSAMessage = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetSPScrimRole()
        {
            return ulong.Parse(conf.scrimSPRole);
        }

        public void SetSPScrimRole(ulong role)
        {
            conf.scrimSPRole = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetSPScrimActiveRole()
        {
            return ulong.Parse(conf.scrimSPActiveRole);
        }

        public void SetSPScrimActiveRole(ulong role)
        {
            conf.scrimSPActiveRole = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetSPScrimChannel()
        {
            return ulong.Parse(conf.scrimSPSignUpChannel);
        }

        public ulong GetSPScrimMessage()
        {
            return ulong.Parse(conf.scrimSPMessage);
        }

        public void SetSPScrimMessage(ulong role)
        {
            conf.scrimSPMessage = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetAUScrimRole()
        {
            return ulong.Parse(conf.scrimAURole);
        }

        public void SetAUScrimRole(ulong role)
        {
            conf.scrimAURole = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetAUScrimActiveRole()
        {
            return ulong.Parse(conf.scrimAUActiveRole);
        }

        public void SetAUScrimActiveRole(ulong role)
        {
            conf.scrimAUActiveRole = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetAUScrimChannel()
        {
            return ulong.Parse(conf.scrimAUSignUpChannel);
        }

        public ulong GetAUScrimMessage()
        {
            return ulong.Parse(conf.scrimAUMessage);
        }

        public void SetAUScrimMessage(ulong role)
        {
            conf.scrimAUMessage = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetScrimAdminRole()
        {
            return ulong.Parse(conf.scrimAdminRole);
        }

        public void SetScrimAdminRole(ulong role)
        {
            conf.scrimAdminRole = role.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetScrimAdminChannel()
        {
            return ulong.Parse(conf.scrimAdminChannel);
        }

        public ulong GetDashboardMessage()
        {
            return ulong.Parse(conf.dashboardMessage);
        }

        public void SaveDashboardMessage(RestUserMessage message)
        {
            conf.dashboardMessage = message.Id.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetScrimChannel()
        {
            return ulong.Parse(conf.scrimChannel);
        }

        public void SaveScrimMessage(RestUserMessage message)
        {
            conf.scrimMessage = message.Id.ToString();

            using (StreamWriter sw = new StreamWriter(configPath, false))
            {
                sw.WriteLine(JsonConvert.SerializeObject(conf));
            }
            using (StreamReader reader = new StreamReader(configPath))
            {
                conf = JsonConvert.DeserializeObject<Config>(reader.ReadLine());
            }
        }

        public ulong GetScrimMessage()
        {
            return ulong.Parse(conf.scrimMessage);
        }

        public ulong GetScrimAdminLogsChannel()
        {
            return ulong.Parse(conf.scrimAdminLogsChannel);
        }

        public int GetMaxScrimSize()
        {
            return int.Parse(conf.scrimSize);
        }

        public void SetMaxScrimSize(int size)
        {
            conf.scrimSize = size.ToString();

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
