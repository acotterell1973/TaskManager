using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Attributes;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Task.Common;
using Task.UPCDB;
using Task.UPCDB.Models;
using static System.String;
using System.Threading.Tasks;

namespace Task.UpcDb.Tasks
{
    [ScopedDependency(ServiceType = typeof(IScheduledTask))]
    public sealed class Import : BaseSingleThreadedTask
    {
        private const string taskCode = "UPCDB_IMPORT";
        private string _fileName;
        private readonly string _runPath;
        private ImageService _imageService = new ImageService();
        private static Random rng = new Random(Environment.TickCount);
        private bool _fileExists;


        private WineHunterContext _context;

        public Import() : base(taskCode)
        {
            _runPath = @"\" + taskCode + @"\";
            _fileName = _runPath + _fileName;
            var di = new DirectoryInfo(_runPath);
            if (!di.Exists) di.Create();

        }

        public override string TaskCode => taskCode;
        public override string TaskName => "imports the data collected from upcdb.com in a csv and iport into the database";
        public override string TaskDescription => "imports the data from upcdb.com";


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
                return true;
            }

            return false;


        }

        private StreamWriter _processLog;
        public override bool Run()
        {

            CloudStorageAccount account;
            CloudStorageAccount.TryParse("DefaultEndpointsProtocol=https;AccountName=winehunter;AccountKey=tuG0LI1tGsBilE+R8GnG0PlWCFvtoULCOwh/IeFydllu7Onc0k4coRXiCFS3d4bDmcBc4oVdBR951PuAW0NjTw==;", out account);
            var queueClient = account.CreateCloudQueueClient();
            // Retrieve a reference to a queue
            var importDataQueue = queueClient.GetQueueReference("winelistjson");
            // Peek at the next message
            //   CloudQueueMessage peekedMessage = importDataQueue.PeekMessage();

            _processLog = File.AppendText(_fileName);
            //    _taskDependencies.Diagnostics.Log("","");
            _context = new WineHunterContext();
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            var maxQueueSize = 32;
            importDataQueue.FetchAttributes();
            var queueCount = importDataQueue.ApproximateMessageCount;
            int loopCount = queueCount.GetValueOrDefault() / maxQueueSize;
            if (queueCount < maxQueueSize) loopCount = queueCount.GetValueOrDefault();
            for (var i = 0; i < loopCount; i++)
            {
                foreach (CloudQueueMessage message in importDataQueue.GetMessages(maxQueueSize, TimeSpan.FromMinutes(5)))
                {
                    var wineInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<UpcDbModel>(message.AsString);
                    var wineVariety = GetWineVariety(wineInfo.Varietal);
                    if (wineVariety == null) continue;
                    var existingItem =
                        _context.WineList.Where(item => item.WineVarietiesVarietyId == wineVariety.VarietyId
                                                      && item.Producer == wineInfo.Winery
                                                      && item.Vintage == wineInfo.Year).ToList();

                    if (existingItem.Any())
                    {
                        _processLog.WriteLine("Existing item: " + wineInfo.WineName + " -  " + wineInfo.Varietal + ", " +
                                              wineInfo.Winery);
                        if (!IsNullOrEmpty(wineInfo.UpcCode))
                        {
                            foreach (var wineList in existingItem)
                            {
                                wineList.Upc = wineInfo.UpcCode;
                                wineList.Size = wineInfo.Size;
                                wineList.AlchoholLevel = wineInfo.AlchoholLevel;
                                _context.SaveChanges();
                                Console.Write("U");
                            }
                        }
                        importDataQueue.DeleteMessage(message);
                        continue;
                    }
                    var wineItem = SaveWineItem(wineInfo, wineVariety);

                    SaveWineRating(wineItem.WineListId, wineInfo);
                    var imageTask = System.Threading.Tasks.Task.Run(async () =>
                    {
                        if (!IsNullOrEmpty(wineInfo.ImagePath))
                        {
                            await UploadImage(wineInfo.ImagePath.Replace("////", "//"), wineItem.Upc, _runPath);
                        }

                    });
                    imageTask.Wait();

                    // Process all messages in less than 5 minutes, deleting each message after processing.
                    importDataQueue.DeleteMessage(message);
                }
            }


            //var processResults = new List<System.Threading.Tasks.Task>();

            //var queueCount = importDataQueue.ApproximateMessageCount;
            //int loopCount = queueCount.GetValueOrDefault() / maxQueueSize;


            //_context = new WineHunterContext();
            //_context.ChangeTracker.AutoDetectChangesEnabled = false;
            //var currentWineList = await _context.WineList.ToListAsync();
            //List<WineVarieties> wineCategories = await _context.WineVarieties.ToListAsync();

            //for (var i = 0; i < loopCount; i++)
            //{
            //    ImportTask(importDataQueue, maxQueueSize, currentWineList, wineCategories).Wait();
            //    Console.WriteLine($"Task Count Added: {i}");
            //    currentWineList = await _context.WineList.ToListAsync();

            //}
            //await System.Threading.Tasks.Task.WhenAll(processResults);

            return true;
        }




        #region Helpers

        private WineVarieties GetWineVariety(string varietyName)
        {
            if (varietyName == null) return null;
            var fixedName = varietyName.Replace("What's in the Bottle", String.Empty);
            var wineVarieties = _context.WineVarieties.ToList();

            foreach (var wineVariety in wineVarieties)
            {
                var internalName = wineVariety.Name.ToLower().Replace(" ", Empty);
                var externalName = fixedName.ToLower().Replace(" ", Empty);
                if (externalName.Contains(internalName))
                {
                    _processLog.WriteLine("Found Variety: " + internalName);
                    return wineVariety;
                }
            }

            WineVarieties grapes = new WineVarieties() { Name = fixedName };
            _context.WineVarieties.Add(grapes);
            _context.SaveChanges();
            _processLog.WriteLine("Adding Variety: " + grapes.Name);
            return grapes;
        }

        private void SaveWineRating(int wineListId, UpcDbModel wineInfo)
        {
            if (IsNullOrEmpty(wineInfo.Rating)) return;
            ;
            var stringLength = wineInfo.Rating.Length;
            if (stringLength > 4) stringLength = 4;
            var wineRating = new WineRatings
            {
                WineListWineListId = wineListId,
                Prefix = wineInfo.Rating.Substring(0, stringLength)
            };
            _context.WineRatings.Add(wineRating);
            _context.SaveChanges();
            _processLog.WriteLine("Adding Ratings: " + wineRating.Prefix);

        }

        private WineList SaveWineItem(UpcDbModel wineInfo, WineVarieties wineVariety)
        {
            var wineData = new WineList()
            {
                Upc = wineInfo.UpcCode,
                AlchoholLevel = (int?)wineInfo.AlchoholLevel,
                WineVarietiesVarietyId = wineVariety.VarietyId,
                Region = wineInfo.Region,
                Producer = wineInfo.Winery ?? "N/A",
                Vintage = wineInfo.Year,
                Size = wineInfo.Size,
            };

            if (wineData.Upc.IsNullOrEmpty())
            {
                wineData.Upc = GetInternalBarCode();
            }

            if (wineData.Region.IsNullOrEmpty())
            {
                wineData.Region = string.Empty;
            }

            if (wineData.Size == 0) wineData.Size = 750;

            wineData.CreatedDate = DateTime.UtcNow;
            _context.WineList.Add(wineData);
            _context.SaveChanges();

            _processLog.WriteLine("UPC: " + wineData.Upc + " Saving item: " + wineInfo.WineName + " -  " + wineInfo.Varietal + ", " + wineInfo.Winery);
            Console.Write(".");
            return wineData;
        }

        private async Task<bool> UploadImage(string imageUrl, string upc, string imagePath)
        {
            var imageData = await _imageService.CreateUploadedImage(imageUrl, upc, imagePath);
            if (imageData == null) return false;
            await _imageService.AddImageToBlobStorageAsync(imageData);
            _processLog.WriteLine("Saved Image: " + imageUrl);
            return true;

        }
        private async System.Threading.Tasks.Task ImportTask(CloudQueue importDataQueue, int maxQueueSize, List<WineList> currentList, List<WineVarieties> wineCategories)
        {

            //await System.Threading.Tasks.Task.Run(async () =>
            //{
            //    // var context = new WinejournaldbContext();
            //    //   context.ChangeTracker.AutoDetectChangesEnabled = false;
            //    var items = importDataQueue.GetMessages(maxQueueSize);
            //    foreach (var data in items.ToList())

            //    {
            //        var wineInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<UpcDbModel>(data.AsString);

            //        if (currentList.Any(w => w.Upc == wineInfo.UpcCode) == false)
            //        {

            //            //Todo: Get CategoryId or insert new one
            //            var category = wineCategories.Where(wc => wineInfo.Category != null && wc.Name.ToLower().Replace(" ", Empty) ==
            //                                                       wineInfo.Category.ToLower().Replace(" ", Empty)).ToList();
            //            // var category = categoryList.First();
            //            WineVarieties wineCategory = new WineVarieties();
            //            if (!category.Any() && wineInfo.Category != null)
            //            {
            //                if (!wineInfo.Category.IsNullOrEmpty())
            //                {
            //                    try
            //                    {
            //                        wineCategory = new WineVarieties { Name = wineInfo.Category };
            //                        _context.WineVarieties.Add(wineCategory);
            //                        await _context.SaveChangesAsync();

            //                        wineCategory.VarietyId = wineCategories.Last().VarietyId + 1;
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        Console.WriteLine(ex.Message + " ----- " + wineInfo.Category);
            //                    }

            //                }
            //            }
            //            else
            //            {
            //                if (category.Any())
            //                    wineCategory = category?.First();
            //            }
            //            wineCategories = await _context.WineVarieties.ToListAsync();

            //            var wineData = new WineList()
            //            {
            //                Upc = wineInfo.UpcCode,
            //                AlchoholLevel = (int?)wineInfo.AlchoholLevel,
            //                WineVarietiesVarietyId = wineCategory.VarietyId,
            //                //   Rating = wineInfo.Rating,
            //                Region = wineInfo.Region,
            //                Producer = wineInfo.Winery ?? "N/A",
            //                Vintage = wineInfo.Year,
            //                Size = wineInfo.Size,
            //            };

            //            if (wineData.Upc.IsNullOrEmpty())
            //            {
            //                wineData.Upc = GetInternalBarCode();
            //            }

            //            if (wineData.Region.IsNullOrEmpty())
            //            {
            //                wineData.Region = string.Empty;
            //            }

            //            _context.WineList.Add(wineData);
            //            Console.Write(".");
            //            try
            //            {
            //                await _context.SaveChangesAsync();
            //                var imageData = await _imageService.CreateUploadedImage(wineInfo.ImagePath, wineData.Upc);
            //                await _imageService.AddImageToBlobStorageAsync(imageData);
            //                await importDataQueue.DeleteMessageAsync(data);
            //            }
            //            catch (Exception exception)
            //            {
            //                Console.ForegroundColor = ConsoleColor.Red;
            //                Console.WriteLine(exception.InnerException?.Message);
            //                Console.ResetColor();
            //                using (var _processLog = File.AppendText(@"C:\GIT\Task.Manager\ImportError.csv"))
            //                {
            //                    await _processLog.WriteLineAsync($"{data.AsString} :: {exception.Message}");
            //                }
            //            }
            //        }
            //        else
            //        {
            //            await importDataQueue.DeleteMessageAsync(data);
            //        }
            //    }


            //    Console.WriteLine("finised");
            //});

        }

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
            var barcode = Regex.Replace(temp, "[a-zA-Z]", string.Empty).Substring(0, 7);
            return "9999-" + barcode;
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

        #endregion
    }
}