using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Task.UpcDb.Tasks;

namespace Task.UPCDB.Tasks
{
	public class DigitEyes
	{
		public DigitEyes()
		{

		}

		private DigitEyesModels GetUpcData(string code)
		{
			DigitEyesModels product = null;
			Task<bool> process = System.Threading.Tasks.Task.Run(async () =>
			{
				using (var client = new HttpClient())
				{
					// New code:
					client.BaseAddress = new Uri("https://www.digit-eyes.com/gtin/v2_0/?");
					client.DefaultRequestHeaders.Accept.Clear();
					client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

					HttpResponseMessage response = await client.GetAsync($"?upcCode={code}&field_names=all&language=en&app_key=/9OpCSXf98Kx&signature=MJEnBfSeAuFx1Bb4fDydahT9YbY=");
					if (response.IsSuccessStatusCode)
					{
						product = await response.Content.ReadAsAsync<DigitEyesModels>();
						//Console.WriteLine("{0}\t${1}\t{2}", product.Name, product.Price, product.Category);
					}
				}
				return true;
			});

			process.Wait();
			return product;
		}

		public UpcDbModel ScrapeWineDetail(string page, UpcDbModel productInfo)
		{
			//Category:
			//	White Wine

			//Varietal:
			//Cortese

			//Region:
			//Italy » Piedmont » Gavi

			//Producer:
			//La Scolca
			var getHtmlWeb = new HtmlWeb();
			var document = getHtmlWeb.Load(page);
			
			var upcNodes = document.DocumentNode.SelectNodes("//div[@class='characteristicsArea']//a");
			productInfo.Category = upcNodes[0].InnerText;
			productInfo.Varietal = upcNodes[1].InnerText;
			productInfo.Region = $"{upcNodes[2].InnerText} / {upcNodes[3].InnerText} / {upcNodes[4].InnerText}";
			productInfo.Winery = upcNodes[5].InnerText;

			//item title - itemTitle
			var upcTitleNodes = document.DocumentNode.SelectNodes("//span[@class='title']");
			productInfo.WineName = upcTitleNodes[0].InnerText.Replace(productInfo.Winery, string.Empty).Trim();
			productInfo.Year = Convert.ToInt32(productInfo.WineName.Substring(productInfo.WineName.Length - 4));
			productInfo.WineName = productInfo.WineName.Replace(productInfo.Year.ToString(), string.Empty);

			return productInfo;

		}

		public bool Run()
		{
			var digitEyesProductInfo = GetUpcData("0089744756510");
			digitEyesProductInfo.product_web_page = "http://www.vinerepublic.com/r/products/la-scolca-gavi-di-gavi-black-label-2011";
			var productInfo = new UpcDbModel();
			productInfo.UpcCode = digitEyesProductInfo.upc_code;
			productInfo.Size = digitEyesProductInfo.uom != null ? Convert.ToInt32(digitEyesProductInfo.uom?.Replace("ML",string.Empty)) : 750;
			productInfo.WineName = digitEyesProductInfo.description;
			var x = ScrapeWineDetail(digitEyesProductInfo.product_web_page, productInfo);
			return true;
		}
	}
}
