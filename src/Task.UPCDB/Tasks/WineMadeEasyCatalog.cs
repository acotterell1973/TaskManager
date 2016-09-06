using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Attributes;
using HtmlAgilityPack;
using Task.Common;
using Task.UpcDb;


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


        public override bool Run()
		{
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
