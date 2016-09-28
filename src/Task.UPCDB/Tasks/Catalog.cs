using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using Attributes;
using HtmlAgilityPack;
using Task.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Task.UpcDb.Tasks
{
    //public class GroundUpDbContext : DbContext
    //{
    //    public DbSet<TodoItem> Todos { get; set; }

    //    protected override void OnConfiguring(DbContextOptions builder)
    //    {
    //        builder.UseSqlServer(@"Server=(localdb)\v11.0;Database=TodoItems;Trusted_Connection=True;");
    //    }
    //}

    [ScopedDependency(ServiceType = typeof(IScheduledTask))]
    public sealed class Catalog : BaseSingleThreadedTask
    {
        private const string taskCode = "UPCDB";
        private string _fileName = "upc.csv";
        private readonly string _runPath = @"c:\";
        public Catalog() : base(taskCode)
        {
            _runPath = @"\" + taskCode + @"\";
        }

        public override string TaskCode => taskCode;
        public override string TaskName => "Scraps the UPC Info from upcdb.com";
        public override string TaskDescription => "Scraps wine data from upcdb.com";

        public override bool Run()
        {
            InsertItemDetailQueue();
            return true;
            for (int i = 0; i < 10; i++)
            {
                GetParentUpcCodes($"http://bottlecount.com/UPCDB/?start={i}");
            }
            return true;
        }

        public override IEnumerable<ArgumentDescriptor> ArgumentDescriptors => new[]
        {
            new ArgumentDescriptor
            {
                Argument="/filename",
                PostArguments="<filename>",
                Description= @"The csv file to create the upc catalog information."
            }
        };

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

        #region Helpers
        public bool GetParentUpcCodes(string page)
        {
            const string baseUrl = "http://bottlecount.com";
            var getHtmlWeb = new HtmlWeb();
            var document = getHtmlWeb.Load(page);

            var upcNodes = document.DocumentNode.SelectNodes("//a")
                .Where(attr => attr.Attributes.Contains("href")
                       && attr.Attributes["href"].Value.Contains("/UPCDB/")
                       && !attr.Attributes["href"].Value.Contains("start"));


            foreach (var upc in upcNodes)
            {
                var fullPath = baseUrl + upc.Attributes["href"].Value;
                GetChildUpcCodes(fullPath);
            }
            return true;
        }
        public bool GetChildUpcCodes(string page)
        {
            const string baseUrl = "http://bottlecount.com";
            var getHtmlWeb = new HtmlWeb();
            var document = getHtmlWeb.Load(page);

            var upcNodes = document.DocumentNode.SelectNodes("//a")
                .Where(attr => attr.Attributes.Contains("href")
                       && attr.Attributes["href"].Value.Contains("/UPCDB/")
                       && !attr.Attributes["href"].Value.Contains("start"));

            using (var processLog = File.AppendText(_fileName))
            {
                foreach (var upc in upcNodes)
                {
                    if (!string.IsNullOrEmpty(upc.Attributes["href"].Value))
                    {
                        var fullPath = baseUrl + upc.Attributes["href"].Value;
                        processLog.WriteLine(fullPath);
                    }
                }
            }

            return true;
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
            Console.WriteLine("555.Item Queue complete.");
            return true;
        }
        #endregion
    }
}