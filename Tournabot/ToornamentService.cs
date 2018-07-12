using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Discord;

namespace Tournabot
{
    public class ToornamentService
    {
        private ConfigHandler config;
        private string authToken;
        public ToornamentService(ConfigHandler conf)
        {
            config = conf;
        }

        public void Request()
        {
            Refresh();
            var client = new RestClient("https://api.toornament.com/organizer/v2/tournaments/1565815665704411136/participants");
            var request = new RestRequest(Method.GET);
            request.AddHeader("authorization", authToken);
            request.AddHeader("range", "participants=0-49");
            request.AddHeader("x-api-key", config.GetToornamentApiKey());
            IRestResponse response = client.Execute(request);
            var memberInfo = JsonConvert.DeserializeObject<List<Participant>>(response.Content);

            //Console.WriteLine("Name: " + memberInfo[48].custom_fields.discord_id);
        }


        public void Refresh()
        {
            var refreshClient = new RestClient("https://api.toornament.com/oauth/v2/token");
            var refreshRequest = new RestRequest(Method.POST);
            refreshRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
            refreshRequest.AddParameter("application/x-www-form-urlencoded", $"grant_type=client_credentials&client_id={config.GetToornamentId()}&client_secret={config.GetToornamentSecret()}&scope=organizer:participant", ParameterType.RequestBody);
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
    }
}
