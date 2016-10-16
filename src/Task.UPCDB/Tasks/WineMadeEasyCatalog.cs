using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Attributes;
using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Polly;
using Task.Common;
using Task.UpcDb;
using Task.UpcDb.Tasks;


namespace Task.UPCDB.Tasks
{
    [ScopedDependency(ServiceType = typeof(IScheduledTask))]
    public class WineMadeEasyCatalog : BaseSingleThreadedTask
    {
        List<string> _pages;
        private StreamWriter _file;
        private const string taskCode = "WINEMADEEASY";
        private string _fileName = "upc.csv";
        private readonly string _fileNameError;
        private readonly string _urlProcessed;
        private readonly string _runPath = @"C:\";
        readonly object _sync = new object();
        private bool _fileExists;

        public WineMadeEasyCatalog() : base(taskCode)
        {

            _pages = new List<string>();
            _runPath += @"\" + taskCode + @"\";
            _fileName = _runPath + _fileName;
            var di = new DirectoryInfo(_runPath);
            if (!di.Exists) di.Create();

            _fileNameError = _runPath + @"\processError.txt";
            _urlProcessed = _runPath + @"\processed.csv";
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

                string pattern = fi.Name.Replace(fi.Extension,string.Empty);
                var matches = Directory.GetFiles(_runPath)
                    .Where(path => Regex.Match(path, pattern).Success).ToList();

                if (!matches.Any())
                {   
                    _fileName =
                        $"{fi.FullName.Replace(fi.Extension, string.Empty)}.{DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss")}{fi.Extension}";
                }
                else
                {
                    _fileName = matches.First();
                    _fileExists = true;
                }
                using (var processLog = File.AppendText(_urlProcessed))
                {
                    processLog.WriteLine("");
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

            lock (_sync)
            {
                using (var processLog = File.AppendText(_urlProcessed))
                {
                    processLog.WriteLine(itemUrl);
                }
                Console.Write("+");
            }
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

            int retries = 0;
            int eventualFailures = 0;

            HtmlDocument document = null;

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetry(
                 retryCount: 3, // Retry 3 times 
                 sleepDurationProvider: attempt => TimeSpan.FromSeconds(5), // Wait 5s between each try. 
                 onRetry: (exception, calculatedWaitDuration) => // Capture some info for logging! 
             {
                 // This is your new exception handler!  
                 // Tell the user what they've won! 

                 var processErrorFile = _runPath + @"\policyError.txt";
                 var fi = new FileInfo(processErrorFile);
                 processErrorFile =
                     $"{fi.FullName.Replace(fi.Extension, string.Empty)}.{DateTime.Now.ToString("yyyy-MM-dd")}{fi.Extension}";

                 lock (_sync)
                 {
                     using (var processLog = File.AppendText(processErrorFile))
                     {

                         document = null;
                         // processLog.WriteLine($"Retries {retries}");
                         processLog.WriteLine($"Retries {retries} :: Policy logging: {page} :: {exception.Message}");
                     }

                     retries++;
                 }

             });

            try
            {
                policy.Execute(() =>
                {
                    var getHtmlWeb = new HtmlWeb();
                    document = getHtmlWeb.Load(page);
                });


                if (document == null) return null;

                //prem-prod-info
                var upcNodesName = document.DocumentNode.SelectNodes("//div[@class='product-name']//h1");
                var upcNodesRatings = document.DocumentNode.SelectNodes("//div[@class='ratings']");
                var upcNodesRegion =
                    document.DocumentNode.SelectNodes(
                        "//div[@id='prem-prod-info']//div[@id='prem-prod-region']//dl/dd");
                var upcNodesContents =
                    document.DocumentNode.SelectNodes("//div[@id='prem-prod-info']//div[@id='prem-prod-contents']");
                var upcNodesDetails =
                    document.DocumentNode.SelectNodes(
                        "//div[@id='prem-prod-info']//div[@id='prem-prod-details']//dl/dd");
                var upcNodesUpc =
                    document.DocumentNode.SelectNodes(
                        "//div[@id='prem-prod-info']//div[@id='prem-prod-details']//dl/meta");
                var upcNodesUpcImage = document.DocumentNode.SelectNodes("//p[@class='product-image']//img");

                var wine = new UpcDbModel
                {
                    WineName = upcNodesName?[0].InnerText.Replace("\n", string.Empty),
                    Category = upcNodesDetails?[0].InnerText.Replace("\n", string.Empty),
                    Winery = upcNodesDetails?[1].InnerText.Replace("\n", string.Empty),
                    Varietal = upcNodesContents?[0].InnerText.Replace("\n", string.Empty),
                //    Region = upcNodesRegion?[0].InnerText.Replace("\n", string.Empty) + ", " + upcNodesRegion?[1].InnerText.Replace("\n", string.Empty),
                    UpcCode = upcNodesUpc?[0].Attributes["content"].Value.Replace("\n", string.Empty),
                    Rating = upcNodesRatings?[0].InnerText.Replace("\n", string.Empty),
                    ImagePath = upcNodesUpcImage?[0].Attributes["src"].Value.Replace("\n", string.Empty),
                };


                decimal wineSize;
                decimal.TryParse(
                    upcNodesDetails?[2].InnerText.Replace("&nbsp;", string.Empty).Replace("ml.", string.Empty),
                    out wineSize);
                wine.Size = wineSize;

                int wineYear;
                int.TryParse(
                    upcNodesDetails?[3].InnerText.Replace("&nbsp;", string.Empty).Replace("ml", string.Empty),
                    out wineYear);
                wine.Year = wineYear;

                if (upcNodesRegion == null) return wine;
                if (!upcNodesRegion.Any()) return wine;
                foreach (var region in upcNodesRegion)
                {
                    wine.Region += region.InnerText.Replace("\n", string.Empty) + ", ";
                }
                var r = wine.Region.TrimEnd(' ').TrimEnd(',');
                wine.Region = r;

                //Region
                return wine;

            }
            catch (Exception exception)
            {
                lock (_sync)
                {
                    using (var processLog = File.AppendText(_fileNameError))
                    {
                        eventualFailures += 1;

                        processLog.WriteLine($"Time:: {DateTime.Now} :: {page} :: {exception.Message} :: failures :: {eventualFailures}");
                        processLog.WriteLine($"stack trace :: {exception}");

                    }

                }
            }

            return null;
        }
        public override bool Run()
        {
            //   var x =  GetUpcData("http://www.winemadeeasy.com/yardstick-cabernet-sauvignon-ruth-s-reach-2010-750-ml-36114.html");
            //    return true;

            var processTask = System.Threading.Tasks.Task.Run(() =>
            {
                var startTime = DateTime.Now;
                DateTime endTime;
                CloudStorageAccount account;
                CloudStorageAccount.TryParse("DefaultEndpointsProtocol=https;AccountName=winehunter;AccountKey=tuG0LI1tGsBilE+R8GnG0PlWCFvtoULCOwh/IeFydllu7Onc0k4coRXiCFS3d4bDmcBc4oVdBR951PuAW0NjTw==;", out account);
                var queueClient = account.CreateCloudQueueClient();
                // Retrieve a reference to a queue
                var shopsImportDataQueue = queueClient.GetQueueReference("winelistjson");

                if (!_fileExists)
                {
 
                    
                    startTime = DateTime.Now;
                    Parallel.For(0, 54, idx =>
                    {
                        var pg = GetPageUrls(idx, 100);
                        Console.WriteLine($"Thread id {System.Threading.Tasks.Task.CurrentId} adding results {idx}");
                        _pages.AddRange(pg);
                    });

                    endTime = DateTime.Now;
                    Console.WriteLine("Page Scrape Duration " + endTime.Subtract(startTime).TotalMinutes);

                    startTime = DateTime.Now;
                    lock (_sync)
                    {
                        File.AppendAllLines(_fileName, _pages);
                        Console.WriteLine("Pages saved to " + _fileName);
                    }
                }
                if (!_pages.Any())
                {
                    var processedPages = (from line in ReadFrom(_urlProcessed)
                              select line).ToList();

                    _pages = (from line in ReadFrom(_fileName)
                              where !processedPages.Contains(line)
                                          select line).ToList();
                }
                // Create the queue if it doesn't already exist
                shopsImportDataQueue.CreateIfNotExists();
                Parallel.ForEach(_pages, page =>
                {
                    InsertItemDetailRowQueue(shopsImportDataQueue, page);
                    // using the lock is the same as the for loop in this parallel case

                });

                endTime = DateTime.Now;
                Console.WriteLine("Page Detail Data Duration " + endTime.Subtract(startTime).TotalMinutes);

            });

            processTask.Wait();

            return true;
        }


    }
}
