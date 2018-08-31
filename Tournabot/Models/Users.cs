using System;
using System.Collections.Generic;

namespace Tournabot.Models
{
    public partial class Users
    {
        public string DiscordTag { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
        public bool SignedUp { get; set; }
        public bool CheckedIn { get; set; }
    }
}
