using System;
using System.Collections.Generic;

namespace Task.UPCDB.Models
{
    public partial class WineVarieties
    {
        public WineVarieties()
        {
            WineList = new HashSet<WineList>();
            WineVarietyTyes = new HashSet<WineVarietyTyes>();
        }

        public int VarietyId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<WineList> WineList { get; set; }
        public virtual ICollection<WineVarietyTyes> WineVarietyTyes { get; set; }
    }
}
