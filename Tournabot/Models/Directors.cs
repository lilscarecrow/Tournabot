using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tournabot.Models
{
    public partial class Directors
    {
        [Column(TypeName = "long")]
        public ulong Id { get; set; }
        public string DirectorName { get; set; }
        [Column(TypeName = "long")]
        public ulong MatchId { get; set; }
        public bool Submitted { get; set; }
    }
}
