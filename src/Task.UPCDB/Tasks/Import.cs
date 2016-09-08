using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Task.Common;
using Task.UPCDB.Models;
using static System.String;

namespace Task.UpcDb.Tasks
{
    [ScopedDependency(ServiceType = typeof(IScheduledTask))]
    public sealed class Import : BaseSingleThreadedTask
    {
        private const string taskCode = "UPCDB_IMPORT";
        private string _fileName;
        private readonly string _runPath;

        private WinejournaldbContext _context;

        public Import() : base(taskCode)
        {
            _runPath = @"\" + taskCode + @"\";
        }

        public override string TaskCode => taskCode;
        public override string TaskName => "imports the data collected from upcdb.com in a csv and iport into the database";
        public override string TaskDescription => "imports the data from upcdb.com";

        public override bool Run()
        {
            var task = System.Threading.Tasks.Task.Run(async () =>
            {
                CloudStorageAccount account;
                CloudStorageAccount.TryParse("DefaultEndpointsProtocol=https;AccountName=winehunter;AccountKey=tuG0LI1tGsBilE+R8GnG0PlWCFvtoULCOwh/IeFydllu7Onc0k4coRXiCFS3d4bDmcBc4oVdBR951PuAW0NjTw==;", out account);
                var queueClient = account.CreateCloudQueueClient();
                // Retrieve a reference to a queue
                var importDataQueue = queueClient.GetQueueReference("winelistjson");
                importDataQueue.FetchAttributes();
                var processResults = new List<System.Threading.Tasks.Task>();
                var maxQueueSize = 32;
                var queueCount = importDataQueue.ApproximateMessageCount;
                int loopCount = queueCount.GetValueOrDefault() / maxQueueSize;


                _context = new WinejournaldbContext();
                _context.ChangeTracker.AutoDetectChangesEnabled = false;
                var currentWineList = await _context.WineList.ToListAsync();
                List<WineCategories> wineCategories = await _context.WineCategories.ToListAsync();

                for (var i = 0; i < loopCount; i++)
                {
                    ImportTask(importDataQueue, maxQueueSize, currentWineList, wineCategories).Wait();
                    Console.WriteLine($"Task Count Added: {i}");
                    currentWineList = await _context.WineList.ToListAsync();
                    wineCategories = await _context.WineCategories.ToListAsync();
                }
                await System.Threading.Tasks.Task.WhenAll(processResults);

            });

            task.Wait();
            return true;
        }


        private async System.Threading.Tasks.Task ImportTask(CloudQueue importDataQueue, int maxQueueSize, List<WineList> currentList, List<WineCategories> wineCategories)
        {

            await System.Threading.Tasks.Task.Run(async () =>
           {
               // var context = new WinejournaldbContext();
               //   context.ChangeTracker.AutoDetectChangesEnabled = false;
               var items = importDataQueue.GetMessages(maxQueueSize);
               foreach (var data in items.ToList())

               {
                   var wineInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<UpcDbModel>(data.AsString);

                   if (currentList.Any(w => w.UpcCode == wineInfo.UpcCode) == false)
                   {

                       //Todo: Get CategoryId or insert new one
                       var category = wineCategories.First(wc =>
                               wc.Name.ToLower().Replace(" ", Empty) ==
                               wineInfo.Category.ToLower().Replace(" ", Empty));

                       if (category == null && wineInfo.Category != null)
                       {
                           if (!wineInfo.Category.IsNullOrEmpty())
                           {
                               category = new WineCategories { Name = wineInfo.Category };
                               _context.WineCategories.Add(category);
                               category.WineCategoryId = await _context.SaveChangesAsync();
                           }
                       }

                       
                       var wineData = new WineList()
                       {
                           UpcCode = wineInfo.UpcCode,
                           Varietal = wineInfo.Varietal,
                           AlchoholLevel = (decimal?)wineInfo.AlchoholLevel,
                           WineCategoryId = category?.WineCategoryId,
                           Rating = wineInfo.Rating,
                           Region = wineInfo.Region,
                           WineName = wineInfo.WineName,
                           Winery = wineInfo.Winery,
                           Year = wineInfo.Year,
                           Size = wineInfo.Size,
                       };

                       _context.WineList.Add(wineData);
                       Console.Write(".");
                       try
                       {
                           await _context.SaveChangesAsync();
                           await importDataQueue.DeleteMessageAsync(data);
                       }
                       catch (Exception exception)
                       {
                           Console.ForegroundColor = ConsoleColor.Red;
                           Console.WriteLine(exception.Message);
                           Console.ResetColor();
                       }
                   }
                   else
                   {
                       await importDataQueue.DeleteMessageAsync(data);
                   }
               }


               Console.WriteLine("finised");
           });

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

        private static Random rng = new Random(Environment.TickCount);

        private static void GetNumber(int objlength)
        {
            for (int index = 0; index < 20; index++)
            {
                int length = Convert.ToInt32(objlength);
                var number = rng.NextDouble().ToString("0.000000000000").Substring(2, length);
                Console.WriteLine(number);
            }
        }

        public static string GetInternalBarCode()
        {
            var temp = Guid.NewGuid().ToString().Replace("-", string.Empty);
            var barcode = Regex.Replace(temp, "[a-zA-Z]", string.Empty).Substring(0, 12);
            return barcode;
        }
        public static string GenerateRandomString(int size)
        {
            Random random = new Random((int)DateTime.Now.Ticks);//thanks to McAden
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }
        #region Helpers


        #endregion
    }
}