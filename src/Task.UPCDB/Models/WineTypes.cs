using System;
using System.Collections.Generic;

namespace Task.UPCDB.Models
{
    public partial class WineTypes
    {
        public WineTypes()
        {
            WineVarietyTyes = new HashSet<WineVarietyTyes>();
        }

        public int WineTypeId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<WineVarietyTyes> WineVarietyTyes { get; set; }
    }
}
