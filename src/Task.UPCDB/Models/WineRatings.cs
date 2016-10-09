using System;
using System.Collections.Generic;

namespace Task.UPCDB.Models
{
    public partial class WineRatings
    {
        public int WineRatingId { get; set; }
        public int WineListWineListId { get; set; }
        public string Prefix { get; set; }
        public decimal Rate { get; set; }

        public virtual WineList WineListWineList { get; set; }
    }
}
