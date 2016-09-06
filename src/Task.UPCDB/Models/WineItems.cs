using System;
using System.Collections.Generic;

namespace Task.UPCDB.Models
{
    public partial class WineItems
    {
        public int WineItemId { get; set; }
        public int WineCategoryId { get; set; }
        public string BinNumber { get; set; }
        public string Producer { get; set; }
        public string Vintage { get; set; }
        public string Region { get; set; }
        public string Price { get; set; }
        public string Occasion { get; set; }
        public string LocationOfOccasion { get; set; }
        public string SharedWith { get; set; }
        public string ServedWith { get; set; }
        public string Comments { get; set; }
        public string Sight { get; set; }
        public string Smell { get; set; }
        public string Taste { get; set; }
        public string OverallImpression { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
    }
}
