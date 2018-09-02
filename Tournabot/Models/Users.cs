using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tournabot.Models
{
    public partial class Users
    {
        [Column(TypeName = "long")]
        public ulong Id { get; set; }
        public string DiscordTag { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
        public bool SignedUp { get; set; }
        public bool CheckedIn { get; set; }
        public bool WaitList { get; set; }
        public int? FirstGame { get; set; }
        public int? SecondGame { get; set; }
        public int? ThirdGame { get; set; }
        public int? Total { get; set; }
        public int Champion { get; set; }
        public bool IsDirector { get; set; }
    }
}
