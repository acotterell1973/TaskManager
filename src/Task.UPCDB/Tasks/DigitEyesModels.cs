using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task.UPCDB.Tasks
{
    
	public class Gcp
	{
		public string postal_code { get; set; }
		public string company { get; set; }
		public object address2 { get; set; }
		public string state { get; set; }
		public string country { get; set; }
		public object fax { get; set; }
		public string phone { get; set; }
		public string gcp { get; set; }
		public string address { get; set; }
		public object contact { get; set; }
		public string gln { get; set; }
		public string city { get; set; }
	}

	public class Manufacturer
	{
		public object postal_code { get; set; }
		public object company { get; set; }
		public object address2 { get; set; }
		public object country { get; set; }
		public object state { get; set; }
		public object phone { get; set; }
		public object address { get; set; }
		public object contact { get; set; }
		public object city { get; set; }
	}

	public class DigitEyesModels
	{
		public string brand { get; set; }
		public string usage { get; set; }
		public string return_message { get; set; }
		public object formattedNutrition { get; set; }
		public Gcp gcp { get; set; }
		public string uom { get; set; }
		public string description { get; set; }
		public Manufacturer manufacturer { get; set; }
		public string return_code { get; set; }
		public object ingredients { get; set; }
		public string image { get; set; }
		public string product_web_page { get; set; }
		public string website { get; set; }
		public string gcp_gcp { get; set; }
		public object nutrition { get; set; }
		public string upc_code { get; set; }
		public string language { get; set; }
	}

}
