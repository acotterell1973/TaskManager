using System;
using System.Collections.Generic;

namespace Task.UPCDB.Models
{
    public partial class WineList
    {
        public WineList()
        {
            WineJournals = new HashSet<WineJournals>();
            WineRatings = new HashSet<WineRatings>();
        }

        public int WineListId { get; set; }
        public int WineVarietiesVarietyId { get; set; }
        public string Upc { get; set; }
        public string Producer { get; set; }
        public string Region { get; set; }
        public int Vintage { get; set; }
        public int? Size { get; set; }
        public int? AlchoholLevel { get; set; }
        public bool? HasSulphites { get; set; }
        public DateTime CreatedDate { get; set; }

        public virtual ICollection<WineJournals> WineJournals { get; set; }
        public virtual ICollection<WineRatings> WineRatings { get; set; }
        public virtual WineVarieties WineVarietiesVariety { get; set; }
    }
}
