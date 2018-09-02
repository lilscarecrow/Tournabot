using System;
using System.Collections.Generic;

namespace Tournabot.Models
{
    public partial class MatchA
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsDirector { get; set; }
        public int? Score { get; set; }
    }
}
