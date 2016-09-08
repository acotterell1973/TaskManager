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
        private string _fileNameError;
        private readonly string _runPath = @"c:\";

        public WineMadeEasyCatalog() : base(taskCode)
        {
            _pages = new List<string>();
            _runPath += @"\" + taskCode + @"\";
            _fileName = @"C:\GIT\Task.Manager\" + _fileName;
            _fileNameError = @"C:\GIT\Task.Manager\processError.csv";
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
        private async Task<bool> InsertItemDetailQueue()
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
                var wine = await GetUpcData(itemUrl);
                if (wine == null) continue;
                var value = Newtonsoft.Json.JsonConvert.SerializeObject(wine);
                var message = new CloudQueueMessage(value);
                await shopsImportDataQueue.AddMessageAsync(message);
            }

            Console.Write("Item Queue complete.");
            return true;
        }

        private async Task<UpcDbModel> GetUpcData(string page)
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
            catch (Exception)
            {
                using (var processLog = File.AppendText(_fileNameError))
                {
                    await processLog.WriteLineAsync(page);
                }
            }
            return null;
        }
        public override bool Run()
        {
            //   var x =  GetUpcData("http://www.winemadeeasy.com/yardstick-cabernet-sauvignon-ruth-s-reach-2010-750-ml-36114.html");
            //    return true;
            var processTask = System.Threading.Tasks.Task.Run(async () =>
            {
                //for (int i = 1; i < 53; i++)
                //{
                //    Console.WriteLine("page " + i);
                //    var pg = await GetPageUrls(i, 100);
                //    Console.WriteLine("adding results " + i);
                //    _pages.AddRange(pg);
                //}
                //using (var processLog = File.AppendText(_fileName))
                //{
                //    foreach (var upc in _pages)
                //    {
                //        await processLog.WriteLineAsync(upc);
                //    }
                //}
                await InsertItemDetailQueue();
            });
            processTask.Wait();

            return true;
        }


    }
}
