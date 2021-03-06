﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Attributes;
using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Polly;
using Task.Common;
using Task.UpcDb;
using Task.UpcDb.Tasks;
using Task.UPCDB.Models;
using Task = System.Threading.Tasks.Task;


namespace Task.UPCDB.Tasks
{
    [ScopedDependency(ServiceType = typeof(IScheduledTask))]
    public class VineRepublicCatalog : BaseSingleThreadedTask
    {
        List<string> _pages;
        private StreamWriter _file;
        private const string taskCode = "VINEREPUBLIX";
        private string _fileName = "upc.csv";
        private string _fileProductUrls = "vineUrls.csv";
        private readonly string _fileNameError;
        private readonly string _urlProcessed;
        private readonly string _runPath = @"C:\";
        readonly object _sync = new object();
        private bool _fileExists;
        private HtmlWeb _getHtmlWeb = new HtmlWeb();
        private WineHunterContext _context;

        public VineRepublicCatalog() : base(taskCode)
        {

            _pages = new List<string>();
            _runPath += @"\" + taskCode + @"\";
            _fileName = _runPath + _fileName;
            _fileProductUrls = _runPath + _fileProductUrls;

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

        private bool InsertItemDetailRowQueue(CloudQueue queue, string itemUrl)
        {
            // ArgumentNullException
            try
            {
                var wine = GetUpcData(itemUrl);
                if (wine == null)
                {
                    //TimeSpan time1 = TimeSpan.FromHours(1);
                    //TimeSpan ts = DateTime.Now.TimeOfDay;
                    //var ts2 = ts.Add(time1);
                    //System.Threading.Tasks.Task.Delay(ts2);
                    //Thread.Sleep(ts2);
                    return false;
                }
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
            catch (ArgumentNullException exception)
            {
                //TimeSpan time1 = TimeSpan.FromHours(1);
                //TimeSpan ts = DateTime.Now.TimeOfDay;
                //var ts2 = ts.Add(time1);
                //System.Threading.Tasks.Task.Delay(ts2);
            }
            return true;
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
                    lock (_sync)
                    {
                        _getHtmlWeb = new HtmlWeb();
                        document = _getHtmlWeb.Load(page);
                    }
                });


                if (document == null) return null;

                var upcNodes = document.DocumentNode.SelectNodes("//div[@class='characteristicsArea']//a");
                if (document.DocumentNode.FirstChild == null) throw new ArgumentNullException(page);
                var category = upcNodes[0].InnerText;

                var varietal = upcNodes[1].InnerText;

                var country = upcNodes.Where(n => n.Attributes["href"].Value.Contains("Country")).Select(n => n.InnerText.Trim());
                var regions = upcNodes.Where(n => n.Attributes["href"].Value.Contains("Region")).Select(n => n.InnerText.Trim());

                var ctry = "";
                var reg = "";
                var enumerable = country as string[] ?? country.ToArray();
                if (enumerable.ToList().Any())
                {
                   ctry= enumerable.ToList().First();
                }
                var enumerable1 = regions as string[] ?? regions.ToArray();
                if (enumerable1.ToList().Any())
                {
                    reg = enumerable1.ToList().First();
                }

                var region = $"{ctry}, {reg}".TrimEnd(Convert.ToChar(","));

                var brandId = upcNodes.Where(n => n.Attributes["href"].Value.Contains("?brandid")).Select(n => n.InnerText.Trim());
                var id = brandId as string[] ?? brandId.ToArray();
                string winery ="";
                if (id.ToList().Any())
                {
                    winery = id.ToList()?.First();
                } 
               

                var alcoholNode = document.DocumentNode.SelectNodes("//div[@class='characteristicsArea']//p");

                //item title - itemTitle
                var upcTitleNodes = document.DocumentNode.SelectNodes("//span[@class='title']");
                var wineName = upcTitleNodes[0].InnerText.Replace(winery, string.Empty).Trim();
                var yearValue =wineName.Substring(wineName.Length - 4);
                int year;
                int.TryParse(yearValue,out year);
                wineName = wineName.Replace(year.ToString(), string.Empty);

                var ratingLf = document.DocumentNode.SelectNodes("//td[@class='reviewIconLeft']")?[0].InnerText;
                var ratingRt = document.DocumentNode.SelectNodes("//td[@class='reviewIconRight']")?[0].InnerText;

                //upc
                var upcNodesUpc =
                    document.DocumentNode.SelectNodes(
                        "//div[@id='prem-prod-info']//div[@id='prem-prod-details']//dl/meta");

                //image
                var upcNodesUpcImage = document.DocumentNode.SelectNodes("//td[@class='imageArea']//img");

                var wine = new UpcDbModel
                {
                    WineName = wineName.Replace("\n", string.Empty),
                    Category = category.Replace("\n", string.Empty),
                    Winery = winery.Replace("\n", string.Empty),
                    Varietal = varietal.Replace("\n", string.Empty),
                    Region = region,
                    UpcCode = upcNodesUpc?[0].Attributes["content"].Value.Replace("\n", string.Empty),
                    Rating = ratingLf + ratingRt,
                    ImagePath = "http://" + upcNodesUpcImage?[0].Attributes["src"].Value.Replace("\n", string.Empty),
                };


                decimal wineSize;
                decimal.TryParse("750".Replace("&nbsp;", string.Empty).Replace("ml.", string.Empty), out wineSize);
                wine.Size = wineSize;

                int wineYear;
                int.TryParse(year.ToString().Replace("&nbsp;", string.Empty).Replace("ml", string.Empty), out wineYear);
                wine.Year = wineYear;
                var r = wine.Region.TrimEnd(' ').TrimEnd(',');
                wine.Region = r;


                foreach (var node in alcoholNode)
                {
                    if (node.InnerText.Contains("%"))
                    {
                        wine.AlchoholLevel = Convert.ToDecimal(node.InnerText.Replace("%", string.Empty));
                    }
                }
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

                        //TimeSpan time1 = TimeSpan.FromHours(1);
                        //TimeSpan ts = DateTime.Now.TimeOfDay;
                        //var ts2 = ts.Add(time1);
                        //System.Threading.Tasks.Task.Delay(ts2);
                    }

                }
            }

            return null;
        }

        private void ExtractLinksFromHtml()
        {
            var pattern = "html";
            var matches = Directory.GetFiles(_runPath).Where(path => Regex.Match(path, pattern).Success).ToList();
            foreach (var fileName in matches)
            {
                var fi = new FileInfo(fileName);
                StreamReader sr = fi.OpenText();
                HtmlDocument doc = new HtmlDocument();
                doc.Load(sr);

                var upcNodes = doc.DocumentNode.SelectNodes("//div[@class='right']//a");
                lock (_sync)
                {
                    File.AppendAllLines(_fileProductUrls, upcNodes.Select(u => u.Attributes["href"].Value));
                }
            }

        }

        private async Task<IEnumerable<VineUrl>> GetUrlsByVarietal(string varietalName)
        {
            if (string.IsNullOrEmpty(varietalName)) return null;
            IEnumerable<VineUrl> productUrls = null;
            using (var client = new HttpClient())
            {
                // New code:
                client.BaseAddress = new Uri("http://www.vinerepublic.com/quick_search");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync($"?term={varietalName}");
                if (response.IsSuccessStatusCode)
                {
                    productUrls = await response.Content.ReadAsAsync<IEnumerable<VineUrl>>();
                    var vineUrls = productUrls as IList<VineUrl> ?? productUrls.ToList();
                    if (vineUrls.Any())
                    {
                        lock (_sync)
                        {
                            File.AppendAllLines(_fileProductUrls, vineUrls.Select(u => "http://www.vinerepublic.com" + u.url));
                        }
                    }

                }
            }

            return productUrls;
        }

        public override bool Run()
        {

            //     ExtractLinksFromHtml();
            ////    var result1 =  GetUpcData("http://www.vinerepublic.com/r/products/10885680/carpineto-vino-nobile-di-montepulciano-riserva-2011");
            //     return true;
            //     var processTask = System.Threading.Tasks.Task.Run(async () =>
            //     {
            //         _context = new WineHunterContext();
            //         var wineVarieties = _context.WineVarieties.ToList();
            //         foreach (var wv in wineVarieties)
            //         {
            //             var result = await GetUrlsByVarietal(wv.Name);
            //         }
            //     });
            //     processTask.Wait();

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


                if (!_pages.Any())
                {
                    var processedPages = (from line in ReadFrom(_urlProcessed)
                                          select line).ToList();

                    _pages = (from line in ReadFrom(_fileName)
                              where !processedPages.Contains(line) && line.Contains("/products/")
                              select line).ToList();


                    _pages = _pages.ToList().Distinct().ToList();
                }
                // Create the queue if it doesn't already exist
                shopsImportDataQueue.CreateIfNotExists();
                Parallel.ForEach(_pages, page =>
                {
                    var result = InsertItemDetailRowQueue(shopsImportDataQueue, page);
                });

                endTime = DateTime.Now;
                Console.WriteLine("Page Detail Data Duration " + endTime.Subtract(startTime).TotalMinutes);

            });

            processTask.Wait();

            return true;
        }


    }

    public class VineUrl
    {
        public string url { get; set; }
        public string label { get; set; }
    }
}
