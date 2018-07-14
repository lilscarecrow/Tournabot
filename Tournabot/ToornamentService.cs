using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tournabot
{
    public class ToornamentService
    {
        private ConfigHandler config;
        private string authToken;
        private Tournament tournamentInfo;

        public ToornamentService(ConfigHandler conf)
        {
            config = conf;
        }

        public async Task GetTournamentInfo()
        {
            Refresh("organizer:view");
            var client = new RestClient($"https://api.toornament.com/organizer/v2/tournaments/{config.GetTournamentSessionId()}");
            var request = new RestRequest(Method.GET);
            request.AddHeader("authorization", authToken);
            request.AddHeader("x-api-key", config.GetToornamentApiKey());
            IRestResponse response = await client.ExecuteTaskAsync(request);
            tournamentInfo = JsonConvert.DeserializeObject<Tournament>(response.Content);
            Console.WriteLine(tournamentInfo.size);
        }

        public async Task<Dictionary<string, bool>> RequestIds()
        {
            await GetTournamentInfo();

            Refresh("organizer:participant");

            var range = 0;

            var client = new RestClient($"https://api.toornament.com/organizer/v2/tournaments/{config.GetTournamentSessionId()}/participants");
            var request = new RestRequest(Method.GET);
            request.AddHeader("authorization", authToken);
            request.AddHeader("range", "participants=0-49");
            request.AddHeader("x-api-key", config.GetToornamentApiKey());
            IRestResponse response1 = await client.ExecuteTaskAsync(request);
            var memberInfo = JsonConvert.DeserializeObject<List<Participant>>(response1.Content);

            foreach(var header in response1.Headers)
            {
                if (header.Name == "Content-Range")
                {
                    range = Int32.Parse(header.Value.ToString().Substring(header.Value.ToString().IndexOf('/') + 1));
                }
            }

            if(range > 49)
            {
                request = new RestRequest(Method.GET);
                request.AddHeader("authorization", authToken);
                request.AddHeader("range", "participants=50-99");
                request.AddHeader("x-api-key", config.GetToornamentApiKey());
                IRestResponse response2 = await client.ExecuteTaskAsync(request);
                memberInfo.AddRange(JsonConvert.DeserializeObject<List<Participant>>(response2.Content));
            }
            Dictionary<string,bool> discordIds = new Dictionary<string, bool>();
            foreach(Participant participant in memberInfo)
            {
                if (participant.custom_fields.discord_id != null && participant.custom_fields.discord_id != "")
                {
                    Regex.Replace(participant.custom_fields.discord_id, @"\s+", "");
                    discordIds.Add(participant.custom_fields.discord_id, participant.checked_in);
                }
            }
            return discordIds;
        }


        public void Refresh(string scope)
        {
            var refreshClient = new RestClient("https://api.toornament.com/oauth/v2/token");
            var refreshRequest = new RestRequest(Method.POST);
            refreshRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
            refreshRequest.AddParameter("application/x-www-form-urlencoded", $"grant_type=client_credentials&client_id={config.GetToornamentId()}&client_secret={config.GetToornamentSecret()}&scope={scope}", ParameterType.RequestBody);
            IRestResponse refreshResponse = refreshClient.Execute(refreshRequest);
            var authTokenObj = JsonConvert.DeserializeObject<AuthTokenObject>(refreshResponse.Content);
            authToken = authTokenObj.token_type + " " + authTokenObj.access_token;
        }

        private class CustomFields
        {
            public string country { get; set; }
            public string discord_id { get; set; }
            public string twitch { get; set; }
            public object joined_discord { get; set; }
        }

        private class Participant
        {
            public CustomFields custom_fields { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public string email { get; set; }
            public string user_id { get; set; }
            public bool checked_in { get; set; }
        }

        private class AuthTokenObject
        {
            public string scope { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
            public string access_token { get; set; }
        }

        private class Logo
        {
            public string logo_small { get; set; }
            public string logo_medium { get; set; }
            public string logo_large { get; set; }
            public string original { get; set; }
        }

        private class Tournament
        {
            public string id { get; set; }
            public string discipline { get; set; }
            public string name { get; set; }
            public string full_name { get; set; }
            public string description { get; set; }
            public string rules { get; set; }
            public string status { get; set; }
            public string participant_type { get; set; }
            public string organization { get; set; }
            public string contact { get; set; }
            public string discord { get; set; }
            public string website { get; set; }
            public bool online { get; set; }
            public string location { get; set; }
            public string country { get; set; }
            public int size { get; set; }
            public string prize { get; set; }
            public string scheduled_date_start { get; set; }
            public string scheduled_date_end { get; set; }
            public string timezone { get; set; }
            public bool @public { get; set; }
            public Logo logo { get; set; }
            public List<string> platforms { get; set; }
            public bool featured { get; set; }
            public bool archived { get; set; }
            public bool match_report_enabled { get; set; }
            public bool registration_enabled { get; set; }
            public DateTime registration_opening_datetime { get; set; }
            public DateTime registration_closing_datetime { get; set; }
            public object registration_notification_enabled { get; set; }
            public object registration_request_message { get; set; }
            public object registration_acceptance_message { get; set; }
            public object registration_refusal_message { get; set; }
            public bool check_in_enabled { get; set; }
            public bool check_in_participant_enabled { get; set; }
            public DateTime check_in_participant_start_datetime { get; set; }
            public DateTime check_in_participant_end_datetime { get; set; }
        }
    }
}
