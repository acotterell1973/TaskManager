using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task.UpcDb.Tasks
{
    public class UpcDbModel
    {
        public string UpcCode { get; set; }
        public string WineName { get; set; }
        public string Winery { get; set; }
        public string Varietal { get; set; }
        public string Region { get; set; }
        public int Year { get; set; }
        public int Size { get; set; }
        public double AlchoholLevel { get; set; }
        public string Rating { get; set; }
		public string Category { get; set; }
	}
}
