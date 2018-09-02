using System;
using System.Collections.Generic;

namespace Tournabot.Models
{
    public partial class Finals
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int? FirstGame { get; set; }
        public int? SecondGame { get; set; }
        public int? ThirdGame { get; set; }
        public int? Total { get; set; }
        public bool IsDirector { get; set; }
    }
}
