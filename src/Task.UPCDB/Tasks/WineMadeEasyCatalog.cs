using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
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
        private StreamWriter _file;
        private const string taskCode = "WINEMADEEASY";
        private string _fileName = "upc.csv";
        private readonly string _fileNameError;
        private readonly string _runPath = @"C:\";

        public WineMadeEasyCatalog() : base(taskCode)
        {
            _pages = new List<string>();
            _runPath += @"\" + taskCode + @"\";
            _fileName = _runPath + _fileName;
            _fileNameError = _runPath + @"\processError.csv";
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
                    Log("/filename argument expects a <filename.csv> value");
                    return false;
                }
                var argVal = argQueue.Dequeue();
                _fileName = _runPath + argVal;

                var fi = new FileInfo(_fileName);
                _fileName =
                    $"{fi.FullName.Replace(fi.Extension, string.Empty)}.{DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss")}{fi.Extension}";

                _file = File.AppendText(_fileName);
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
        private List<string> GetPageUrls(int pageCount, int pageSize)
        {
            List<string> pg;
            string page = $"http://www.winemadeeasy.com/wine/products/?limit={pageSize}&p={pageCount}";
            var getHtmlWeb = new HtmlWeb();
            var document = getHtmlWeb.Load(page);

            var upcNodes = document.DocumentNode.SelectNodes("//ol[@class='products-list']//li//a[@class='product-image']");

            pg = upcNodes.Select(s => s.Attributes["href"].Value).ToList();
            return pg;
        }
        private async Task<List<string>> GetPageUrlsAsync(int pageCount, int pageSize)
        {
            List<string> pg;
            return await System.Threading.Tasks.Task.Run(() =>
            {
                string page = $"http://www.winemadeeasy.com/wine/products/?limit={pageSize}&p={pageCount}";
                var getHtmlWeb = new HtmlWeb();
                var document = getHtmlWeb.Load(page);

                var upcNodes = document.DocumentNode.SelectNodes("//ol[@class='products-list']//li//a[@class='product-image']");

                pg = upcNodes.Select(s => s.Attributes["href"].Value).ToList();
                return pg;

            });
        }
        private async Task<bool> InsertItemDetailQueueAsync()
        {

            CloudStorageAccount account;
            CloudStorageAccount.TryParse("DefaultEndpointsProtocol=https;AccountName=winehunter;AccountKey=tuG0LI1tGsBilE+R8GnG0PlWCFvtoULCOwh/IeFydllu7Onc0k4coRXiCFS3d4bDmcBc4oVdBR951PuAW0NjTw==;", out account);
            var queueClient = account.CreateCloudQueueClient();
            // Retrieve a reference to a queue
            var shopsImportDataQueue = queueClient.GetQueueReference("winelistjson");

            // Create the queue if it doesn't already exist
            shopsImportDataQueue.CreateIfNotExists();

            var itemDetailUrls = (from line in ReadFrom(_fileName)
                                  select line);

            foreach (var itemUrl in itemDetailUrls)
            {
                var wine = await GetUpcDataAsync(itemUrl);
                if (wine == null) continue;
                var value = Newtonsoft.Json.JsonConvert.SerializeObject(wine);
                var message = new CloudQueueMessage(value);
                await shopsImportDataQueue.AddMessageAsync(message);
                Console.WriteLine(".");
            }

            Console.WriteLine("999.Item Queue complete.");
            return true;
        }

        private void InsertItemDetailRowQueue(CloudQueue queue, string itemUrl)
        {
            var wine = GetUpcData(itemUrl);
            if (wine == null) return;
            var value = Newtonsoft.Json.JsonConvert.SerializeObject(wine);
            var message = new CloudQueueMessage(value);
            queue.AddMessage(message);

            Console.Write("+");

        }


        private async Task<UpcDbModel> GetUpcDataAsync(string page)
        {
            var getHtmlWeb = new HtmlWeb();
            var document = getHtmlWeb.Load(page);

            try
            {
                //prem-prod-info
                var upcNodesName = document.DocumentNode.SelectNodes("//div[@class='product-name']//h1");
                var upcNodesRatings = document.DocumentNode.SelectNodes("//div[@class='ratings']");
                var upcNodesRegion = document.DocumentNode.SelectNodes("//div[@id='prem-prod-info']//div[@id='prem-prod-region']//dl/dd");
                var upcNodesContents = document.DocumentNode.SelectNodes("//div[@id='prem-prod-info']//div[@id='prem-prod-contents']");
                var upcNodesDetails = document.DocumentNode.SelectNodes("//div[@id='prem-prod-info']//div[@id='prem-prod-details']//dl/dd");
                var upcNodesUpc = document.DocumentNode.SelectNodes("//div[@id='prem-prod-info']//div[@id='prem-prod-details']//dl/meta");
                var upcNodesUpcImage = document.DocumentNode.SelectNodes("//p[@class='product-image']//img");

                var wine = new UpcDbModel
                {
                    WineName = upcNodesName?[0].InnerText.Replace("\n", string.Empty),
                    Category = upcNodesDetails?[0].InnerText.Replace("\n", string.Empty),
                    Winery = upcNodesDetails?[1].InnerText.Replace("\n", string.Empty),
                    Varietal = upcNodesContents?[0].InnerText.Replace("\n", string.Empty),
                    Region = upcNodesRegion?[0].InnerText.Replace("\n", string.Empty),
                    UpcCode = upcNodesUpc?[0].Attributes["content"].Value.Replace("\n", string.Empty),
                    Rating = upcNodesRatings?[0].InnerText.Replace("\n", string.Empty),
                    ImagePath = upcNodesUpcImage?[0].Attributes["src"].Value.Replace("\n", string.Empty),
                };


                var wineSize = 0;
                int.TryParse(upcNodesDetails?[2].InnerText.Replace("&nbsp;", string.Empty).Replace("ml.", string.Empty), out wineSize);
                wine.Size = wineSize;

                var wineYear = 0;
                int.TryParse(upcNodesDetails?[3].InnerText.Replace("&nbsp;", string.Empty).Replace("ml", string.Empty), out wineYear);
                wine.Year = wineYear;

                return wine;

            }
            catch (Exception exception)
            {
                using (var processLog = File.AppendText(_fileNameError))
                {
                    await processLog.WriteLineAsync($"{page} :: {exception.Message}");
                }
            }
            return null;
        }
        private UpcDbModel GetUpcData(string page)
        {
            var getHtmlWeb = new HtmlWeb();
            var document = getHtmlWeb.Load(page);

            try
            {
                //prem-prod-info
                var upcNodesName = document.DocumentNode.SelectNodes("//div[@class='product-name']//h1");
                var upcNodesRatings = document.DocumentNode.SelectNodes("//div[@class='ratings']");
                var upcNodesRegion = document.DocumentNode.SelectNodes("//div[@id='prem-prod-info']//div[@id='prem-prod-region']//dl/dd");
                var upcNodesContents = document.DocumentNode.SelectNodes("//div[@id='prem-prod-info']//div[@id='prem-prod-contents']");
                var upcNodesDetails = document.DocumentNode.SelectNodes("//div[@id='prem-prod-info']//div[@id='prem-prod-details']//dl/dd");
                var upcNodesUpc = document.DocumentNode.SelectNodes("//div[@id='prem-prod-info']//div[@id='prem-prod-details']//dl/meta");
                var upcNodesUpcImage = document.DocumentNode.SelectNodes("//p[@class='product-image']//img");

                var wine = new UpcDbModel
                {
                    WineName = upcNodesName?[0].InnerText.Replace("\n", string.Empty),
                    Category = upcNodesDetails?[0].InnerText.Replace("\n", string.Empty),
                    Winery = upcNodesDetails?[1].InnerText.Replace("\n", string.Empty),
                    Varietal = upcNodesContents?[0].InnerText.Replace("\n", string.Empty),
                    Region = upcNodesRegion?[0].InnerText.Replace("\n", string.Empty),
                    UpcCode = upcNodesUpc?[0].Attributes["content"].Value.Replace("\n", string.Empty),
                    Rating = upcNodesRatings?[0].InnerText.Replace("\n", string.Empty),
                    ImagePath = upcNodesUpcImage?[0].Attributes["src"].Value.Replace("\n", string.Empty),
                };


                var wineSize = 0;
                int.TryParse(upcNodesDetails?[2].InnerText.Replace("&nbsp;", string.Empty).Replace("ml.", string.Empty), out wineSize);
                wine.Size = wineSize;

                var wineYear = 0;
                int.TryParse(upcNodesDetails?[3].InnerText.Replace("&nbsp;", string.Empty).Replace("ml", string.Empty), out wineYear);
                wine.Year = wineYear;

                return wine;

            }
            catch (Exception exception)
            {
                object sync = new object();
                using (var processLog = File.AppendText(_fileNameError))
                {
                    lock (sync)
                    {
                        processLog.WriteLine($"{page} :: {exception.Message}");
                    }

                }
            }
            return null;
        }
        public override bool Run()
        {
            //   var x =  GetUpcData("http://www.winemadeeasy.com/yardstick-cabernet-sauvignon-ruth-s-reach-2010-750-ml-36114.html");
            //    return true;
            object sync = new object();
            var processTask = System.Threading.Tasks.Task.Run(() =>
          {
              CloudStorageAccount account;
              CloudStorageAccount.TryParse("DefaultEndpointsProtocol=https;AccountName=winehunter;AccountKey=tuG0LI1tGsBilE+R8GnG0PlWCFvtoULCOwh/IeFydllu7Onc0k4coRXiCFS3d4bDmcBc4oVdBR951PuAW0NjTw==;", out account);
              var queueClient = account.CreateCloudQueueClient();
              // Retrieve a reference to a queue
              var shopsImportDataQueue = queueClient.GetQueueReference("winelistjson");

              // Create the queue if it doesn't already exist
              shopsImportDataQueue.CreateIfNotExists();

              var a = DateTime.Now;

              Parallel.For(0, 54, idx =>
              {
                  var pg = GetPageUrls(idx, 100);
                  Console.WriteLine($"Thread id {System.Threading.Tasks.Task.CurrentId} adding results {idx}");
                  _pages.AddRange(pg);
              });

              var b = DateTime.Now;
              Console.WriteLine("Page Scrape Duration" + b.Subtract(a).TotalMinutes);

              a = DateTime.Now;

              Parallel.ForEach(_pages, page =>
              {
                  InsertItemDetailRowQueue(shopsImportDataQueue, page);
                  //using the lock is the same as the for loop in this parallel case
                  lock (sync)
                  {
                      _file.WriteLine(page);
                  }
              });

              b = DateTime.Now;
              Console.WriteLine("Page Detail Data Duration" + b.Subtract(a).TotalMinutes);

          });
            processTask.Wait();

            return true;
        }


    }
}
