﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Task.Common;
using Task.UPCDB.Models;

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
                for (var i = 0; i < loopCount; i++)
                {
                    ImportTask(importDataQueue, maxQueueSize, currentWineList).Wait();
                    Console.WriteLine($"Task Count Added: {i}");
                    currentWineList = await _context.WineList.ToListAsync();
                }
                await System.Threading.Tasks.Task.WhenAll(processResults);
            });

            task.Wait();
            return true;
        }

        private async System.Threading.Tasks.Task ImportTask(CloudQueue importDataQueue, int maxQueueSize, List<WineList> currentList)
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
                       var wineData = new WineList()
                       {
                           UpcCode = wineInfo.UpcCode,
                           Varietal = wineInfo.Varietal,
                           AlchoholLevel = (decimal?)wineInfo.AlchoholLevel,
                           //     CreatedDate = DateTime.Now,
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

        #region Helpers


        #endregion
    }
}