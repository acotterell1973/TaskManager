using System;
using System.Collections.Generic;

namespace Task.UPCDB.Models
{
    public partial class WineList
    {
        public int WineListId { get; set; }
        public string UpcCode { get; set; }
        public string WineName { get; set; }
        public string Winery { get; set; }
        public string Varietal { get; set; }
        public string Region { get; set; }
        public int Year { get; set; }
        public int? Size { get; set; }
        public decimal? AlchoholLevel { get; set; }
        public string Rating { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
