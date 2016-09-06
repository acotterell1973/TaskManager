using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Attributes;
using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Task.Common;
using Task.UpcDb;
using Task.UpcDb.Tasks;


namespace Task.UPCDB.Tasks
{
	[ScopedDependency(ServiceType = typeof(IScheduledTask))]
	public class WineMadeEasyCatalog : BaseSingleThreadedTask
	{
		readonly List<string> _pages;
		private const string taskCode = "WINEMADEEASY";
		private string _fileName = "upc.csv";
		private readonly string _runPath = @"c:\";

		public WineMadeEasyCatalog() : base(taskCode)
		{
			_pages = new List<string>();
            _runPath += @"\" + taskCode + @"\";
		    _fileName = @"C:\GIT\Task.Manager\" + _fileName;
		}

		public override string TaskCode => taskCode;
		public override string TaskName => "Scraps the UPC Info from winemadeeasy.com";
		public override string TaskDescription => "Scraps wine data from winemadeeasy.com";

		public override bool ParseArguments(string[] args)
		{
			var argQueue = new Queue<string>(args);
			while (argQueue.Count > 0)
			{
				var arg = argQueue.Dequeue();
				if (!arg.Contains("/filename")) continue;
				if (argQueue.Count == 0)
				{
					Log("/filename argument expects a filename.csv value");
					return false;
				}
				var argVal = argQueue.Dequeue();
				_fileName = _runPath + argVal;
				var fi = new FileInfo(_fileName);
				if (!fi.Exists)
				{
					fi.CreateText();
				}
				return true;
			}

			return false;
		}

		static IEnumerable<string> ReadFrom(string file)
		{
			string line;
			using (var reader = File.OpenText(file))
			{
				while ((line = reader.ReadLine()) != null)
				{
					string newRecord = line.Replace("\"", "");
					yield return newRecord;
				}
			}
		}

        private async Task<List<string>> GetPageUrls(int pageCount, int pageSize)
        {
            List<string> pg;
            return  await System.Threading.Tasks.Task.Run(() =>
            {
                string page = $"http://www.winemadeeasy.com/wine/products/?limit={pageSize}&p={pageCount}";
                var getHtmlWeb = new HtmlWeb();
                var document = getHtmlWeb.Load(page);

                var upcNodes = document.DocumentNode.SelectNodes("//ol[@class='products-list']//li//a[@class='product-image']");

                pg = upcNodes.Select(s => s.Attributes["href"].Value).ToList();
                return pg;

            });
        }
        private bool InsertItemDetailQueue()
        {
            CloudStorageAccount account;
            CloudStorageAccount.TryParse("DefaultEndpointsProtocol=https;AccountName=winehunter;AccountKey=tuG0LI1tGsBilE+R8GnG0PlWCFvtoULCOwh/IeFydllu7Onc0k4coRXiCFS3d4bDmcBc4oVdBR951PuAW0NjTw==;", out account);
            var queueClient = account.CreateCloudQueueClient();
            // Retrieve a reference to a queue
            var shopsImportDataQueue = queueClient.GetQueueReference("winelistjson");

            // Create the queue if it doesn't already exist
            shopsImportDataQueue.CreateIfNotExists();
            _fileName = @"c:\UPCDB\upc.csv";
            var itemDetailUrls = (from line in ReadFrom(_fileName)
                                  select line);

            foreach (var itemUrl in itemDetailUrls)
            {
                var splits = itemUrl.Split('/');
                //ignore invalid urls
                if (splits.Last().Length == 5 || splits.Last().Length == 6) continue;


                var getHtmlWeb = new HtmlWeb();
                var document = getHtmlWeb.Load(itemUrl);

                var upcNodes = document.DocumentNode.SelectNodes("//tr").Descendants("td").Where(o => o.GetAttributeValue("width", "") == "80%").ToList();
                if (!upcNodes.Any()) continue;
                var wine = new UpcDbModel
                {
                    WineName = upcNodes[0].InnerText.Replace("&nbsp;", string.Empty),
                    Winery = upcNodes[1].InnerText.Replace("&nbsp;", string.Empty),
                    Varietal = upcNodes[2].InnerText.Replace("&nbsp;", string.Empty),
                    Region = upcNodes[3].InnerText.Replace("&nbsp;", string.Empty),
                    UpcCode = upcNodes[12].InnerText.Replace("&nbsp;", string.Empty),
                    Rating = upcNodes[6].InnerText.Replace("&nbsp;", string.Empty)
                };

                var wineSize = 0;
                int.TryParse(upcNodes[9].InnerText.Replace("&nbsp;", string.Empty).Replace("ml", string.Empty), out wineSize);
                wine.Size = wineSize;

                var wineYear = 0;
                int.TryParse(upcNodes[4].InnerText.Replace("&nbsp;", string.Empty).Replace("ml", string.Empty), out wineYear);
                wine.Year = wineYear;


                var value = Newtonsoft.Json.JsonConvert.SerializeObject(wine);
                var message = new CloudQueueMessage(value);
                System.Threading.Tasks.Task.Run(async () =>
                {
                    await shopsImportDataQueue.AddMessageAsync(message);
                    Console.Write("+");
                });

            }
            Console.Write("Item Queue complete.");
            return true;
        }

        private void GetUpcData()
        {
            var getHtmlWeb = new HtmlWeb();
            var document = getHtmlWeb.Load("http://www.winemadeeasy.com/yardstick-cabernet-sauvignon-ruth-s-reach-2010-750-ml-36114.html");
            //prem-prod-info
            var upcNodesName = document.DocumentNode.SelectNodes("//div[@id='product-name']//h1");
            var upcNodesRatings = document.DocumentNode.SelectNodes("//div[@class='ratings']");
            var upcNodesRegion = document.DocumentNode.SelectNodes("//div[@id='prem-prod-info']//div[@id='prem-prod-region']//dl/dd");
            var upcNodesContents = document.DocumentNode.SelectNodes("//div[@id='prem-prod-info']//div[@id='prem-prod-contents']");
            var upcNodesDetails = document.DocumentNode.SelectNodes("//div[@id='prem-prod-info']//div[@id='prem-prod-details']//dl/dd");

            //var wine = new UpcDbModel
            //{
            //    WineName = upcNodes[0].InnerText.Replace("&nbsp;", string.Empty),
            //    Winery = upcNodes[1].InnerText.Replace("&nbsp;", string.Empty),
            //    Varietal = upcNodes[2].InnerText.Replace("&nbsp;", string.Empty),
            //    Region = upcNodes[3].InnerText.Replace("&nbsp;", string.Empty),
            //    UpcCode = upcNodes[12].InnerText.Replace("&nbsp;", string.Empty),
            //    Rating = upcNodes[6].InnerText.Replace("&nbsp;", string.Empty)
            //};

            //var wineSize = 0;
            //int.TryParse(upcNodes[9].InnerText.Replace("&nbsp;", string.Empty).Replace("ml", string.Empty), out wineSize);
            //wine.Size = wineSize;

            //var wineYear = 0;
            //int.TryParse(upcNodes[4].InnerText.Replace("&nbsp;", string.Empty).Replace("ml", string.Empty), out wineYear);
            //wine.Year = wineYear;

        }
        public override bool Run()
        {
            GetUpcData();
            return true;
		    var processTask = System.Threading.Tasks.Task.Run(async () =>
		    {
                for (int i = 1; i < 53; i++)
                {
                    var pg = await GetPageUrls(i, 100);
                    _pages.AddRange(pg);
                }
                using (var processLog = File.AppendText(_fileName))
                {
                    foreach (var upc in _pages)
                    {
                        await processLog.WriteLineAsync(upc);
                    }
                }

            });
		    processTask.Wait();
			
			return true;
		}


	}
}
