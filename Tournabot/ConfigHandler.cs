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
            public string signUpMessage;
            public string checkInMessage;
            public string waitListMessage;
            public string regionMessage;
            public string directorRole;
            public string finalsDirectorRole;
            public string matchA;
            public string matchB;
            public string matchC;
            public string matchD;
            public string matchE;
            public string matchF;
            public string matchG;
            public string matchH;
            public string matchI;
            public string matchJ;
            public string finalist;
            public string champion;
            public string guild;
        }

        public ConfigHandler()
        {
            conf = new Config()
            {
                token = "",
                sql = "",
                bracketInfoChannel = "",
                signUpChannel = "",
                signUpMessage = "",
                checkInMessage = "",
                waitListMessage = "",
                regionMessage = "",
                directorRole = "",
                finalsDirectorRole = "",
                matchA = "",
                matchB = "",
                matchC = "",
                matchD = "",
                matchE = "",
                matchF = "",
                matchG = "",
                matchH = "",
                matchI = "",
                matchJ = "",
                finalist = "",
                champion = "",
                guild = ""
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

        public ulong GetMatchARole()
        {
            return ulong.Parse(conf.matchA);
        }

        public ulong GetMatchBRole()
        {
            return ulong.Parse(conf.matchB);
        }

        public ulong GetMatchCRole()
        {
            return ulong.Parse(conf.matchC);
        }

        public ulong GetMatchDRole()
        {
            return ulong.Parse(conf.matchD);
        }

        public ulong GetMatchERole()
        {
            return ulong.Parse(conf.matchE);
        }

        public ulong GetMatchFRole()
        {
            return ulong.Parse(conf.matchF);
        }

        public ulong GetMatchGRole()
        {
            return ulong.Parse(conf.matchG);
        }

        public ulong GetMatchHRole()
        {
            return ulong.Parse(conf.matchH);
        }

        public ulong GetMatchIRole()
        {
            return ulong.Parse(conf.matchI);
        }

        public ulong GetMatchJRole()
        {
            return ulong.Parse(conf.matchJ);
        }

        public List<ulong> GetMatchRoles()
        {
            List<ulong> roles = new List<ulong>();
            roles.Add(GetMatchARole());
            roles.Add(GetMatchBRole());
            roles.Add(GetMatchCRole());
            roles.Add(GetMatchDRole());
            roles.Add(GetMatchERole());
            roles.Add(GetMatchFRole());
            roles.Add(GetMatchGRole());
            roles.Add(GetMatchHRole());
            roles.Add(GetMatchIRole());
            roles.Add(GetMatchJRole());
            return roles;
        }

        public ulong GetFinalistRole()
        {
            return ulong.Parse(conf.finalist);
        }

        public ulong GetChampionRole()
        {
            return ulong.Parse(conf.champion);
        }

        public ulong GetGuild()
        {
            return ulong.Parse(conf.guild);
        }
    }
}
