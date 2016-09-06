using System;
using System.Collections.Generic;

namespace Task.UPCDB.Models
{
    public partial class WineTerms
    {
        public int WineTermId { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
