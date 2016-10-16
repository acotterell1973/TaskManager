using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Attributes;
using HtmlAgilityPack;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Polly;
using Task.Common;
using Task.UpcDb;
using Task.UpcDb.Tasks;
using Task.UPCDB.Models;


namespace Task.UPCDB.Tasks
{
    [ScopedDependency(ServiceType = typeof(IScheduledTask))]
    public class AlbertsonsCatalog : BaseSingleThreadedTask
    {
        List<string> _pages;
        private StreamWriter _file;
        private const string taskCode = "ALBERTSONS";
        private string _fileName = nameof(AlbertsonsCatalog) + ".csv";
        private readonly string _fileProductUrls = nameof(AlbertsonsCatalog) + "-Urls.csv";
        private readonly string _fileNameError;
        private readonly string _urlProcessed;
        private readonly string _runPath =  @"C:\";
        readonly object _sync = new object();
        private bool _fileExists;

        private WineHunterContext _context;

        public AlbertsonsCatalog() : base(taskCode)
        {
            //var runDrive = PlatformServices.Default.Application.ApplicationBasePath
            _pages = new List<string>();
            _runPath += @"\" + taskCode + @"\";
            _fileName = _runPath + _fileName;
            _fileProductUrls = _runPath + _fileProductUrls;

            var di = new DirectoryInfo(_runPath);
            if (!di.Exists) di.Create();

            _fileNameError = _runPath + $@"\{nameof(AlbertsonsCatalog)}Error.txt";
            _urlProcessed = _runPath + @"\processed.csv";
        }

        public override string TaskCode => taskCode;
        public override string TaskName => "Scraps the UPC Info from http://www.albertsons.com/";
        public override string TaskDescription => "Scraps wine data from http://www.albertsons.com/";

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

                string pattern = fi.Name.Replace(fi.Extension, string.Empty);
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



        private void SaveUrlList(string page)
        {

            var getHtmlWeb = new HtmlWeb();
            var document = getHtmlWeb.Load(page);

            var upcNodes = document.DocumentNode.SelectNodes("//ul[@class='products']//a")
                .Where(attr => attr.Attributes.Contains("href"));

            foreach (var p in upcNodes.Select(upc => upc.Attributes["href"].Value).Select(GetChildUrlList))
            {
                _pages.AddRange(p);
            }
            lock (_sync)
            {
                File.AppendAllLines(_fileName, _pages);
                Console.WriteLine("Pages saved to " + _fileName);
            }

        }
        public List<string> GetChildUrlList(string page)
        {
            var getHtmlWeb = new HtmlWeb();
            var document = getHtmlWeb.Load(page);

            var upcNodes = document.DocumentNode.SelectNodes("//ul[@class='products']//a")
             .Where(attr => attr.Attributes.Contains("href"));

            var htmlNodes = upcNodes as IList<HtmlNode> ?? upcNodes.ToList();

            var pgs = htmlNodes
                .Where(uri => uri.HasAttributes && !string.IsNullOrEmpty(uri.Attributes["href"].Value) )
                .Select(s => s.Attributes["href"].Value).ToList();
            return pgs;

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

                var brandNode = document.DocumentNode.SelectSingleNode("//div[@class='brand']//span");

                var varietalNode = document.DocumentNode.SelectSingleNode("//div/span[@class='name']");
                var imageNode = document.DocumentNode.SelectSingleNode("//div[@class='image']/a/img");
                var sizeNode = document.DocumentNode.SelectSingleNode("//section/p/span[@class='unit']");
                var upcNode = document.DocumentNode.SelectSingleNode("//section/p/span[@class='sku']");

                var wine = new UpcDbModel
                {
              //      WineName = upcNodes[0].InnerText.Replace("&nbsp;", string.Empty),
                    Winery = brandNode?.InnerText.Replace("&nbsp;", string.Empty),
                    Varietal = varietalNode?.InnerText.Replace("&nbsp;", string.Empty),
                  //  Region = upcNodes[3].InnerText.Replace("&nbsp;", string.Empty),
                    UpcCode = upcNode?.InnerText.Replace("SKU / UPC: ", string.Empty),
                    //  Rating = upcNodes[6].InnerText.Replace("&nbsp;", string.Empty)
                    ImagePath = imageNode?.Attributes["src"].Value.Replace("\n", string.Empty),
                };

                var wineSize = 0;
                int.TryParse(sizeNode?.InnerText.Replace(" fl oz", string.Empty).Replace("ml", string.Empty), out wineSize);
                wine.Size = wineSize;

                var wineYear = 0;
              //  int.TryParse(upcNodes[4].InnerText.Replace("&nbsp;", string.Empty).Replace("ml", string.Empty), out wineYear);
                wine.Year = wineYear;
                Console.WriteLine($"saving wine name : {wine.WineName}");
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
            //GetUpcData("http://www.albertsons.com/pd/3-Blind-Moose/Cabernet-Sauvignon/25-40-fl-oz/082100714506/");
            //return false;
            SaveUrlList("http://www.albertsons.com/pd/category/Grocery/Beverages/Wine/637");


            var processTask = System.Threading.Tasks.Task.Run(() =>
            {
                var startTime = DateTime.Now;
                DateTime endTime;
                CloudStorageAccount account;
                CloudStorageAccount.TryParse("DefaultEndpointsProtocol=https;AccountName=winehunter;AccountKey=tuG0LI1tGsBilE+R8GnG0PlWCFvtoULCOwh/IeFydllu7Onc0k4coRXiCFS3d4bDmcBc4oVdBR951PuAW0NjTw==;", out account);
                var queueClient = account.CreateCloudQueueClient();
                // Retrieve a reference to a queue
                var shopsImportDataQueue = queueClient.GetQueueReference("winelistjson");

                _pages = new List<string>();
                var processedPages = (from line in ReadFrom(_urlProcessed)
                                      select line).ToList();

                _pages = (from line in ReadFrom(_fileName)
                          where !processedPages.Contains(line) 
                          select line).Distinct().ToList();

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
