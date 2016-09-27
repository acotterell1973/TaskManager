using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Task.Common;
using Task.Manager;

namespace Task.Manager
{
    public class UpcDbModel
    {
        public string UpcCode { get; set; }
        public string WineName { get; set; }
        public string Winery { get; set; }
        public string Varietal { get; set; }
        public string Region { get; set; }
        public int Year { get; set; }
        public int Size { get; set; }
        public double AlchoholLevel { get; set; }
        public string Rating { get; set; }
    }

    public class Program
    {

        private static Random rng = new Random(Environment.TickCount);

        private static void GetNumber(int objlength)
        {
            for (int index = 0; index < 2; index++)
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
            Random random = new Random((int)DateTime.Now.Ticks); //thanks to McAden
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return "9999-" + builder;
        }


        private static string FormatArgumentDescriptors(IEnumerable<Common.ArgumentDescriptor> args, int columns)
        {
            var sb = new StringBuilder();

            // 2/7ths, pulled directly out of my...
            var descstart = columns * 2 / 7;

            foreach (var arg in args)
            {
                // Write the argument's usage format:
                sb.Append("  ");
                sb.Append(arg.Argument);
                var lineLength = 2 + arg.Argument.Length;

                if (!arg.PostArguments.IsNullOrEmpty())
                {
                    lineLength += 1 + arg.PostArguments.Length;
                    sb.Append(" ");

                    sb.Append(arg.PostArguments);
                }

                if (!arg.Description.IsNullOrEmpty())
                {
                    // Align up to where descriptions should start:
                    if (lineLength > descstart)
                    {
                        sb.AppendLine();
                        sb.Append(new string(' ', descstart));
                    }
                    else
                    {
                        sb.Append(new string(' ', descstart - lineLength));
                    }

                    // Write out the description word-wrapped to the end of the window:
                    sb.AppendLine(string.Join(
                        Environment.NewLine + new string(' ', descstart),
                        arg.Description.WordWrap(columns - descstart - 1).ToArray()
                        ));
                }
                else
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static void DisplayUsageText()
        {
            Console.WriteLine("ApplicationName: {0} {1}", Startup.Application.ApplicationName,
                Startup.Application.ApplicationVersion);
            Console.WriteLine("ApplicationBasePath: {0}", Startup.Application.ApplicationBasePath);
            Console.WriteLine("Framework: {0}", Startup.Application.RuntimeFramework.FullName);
            //    Console.WriteLine("_runtime: {0} {1} {2}", Startup.Runtime.RuntimeType, Startup.Runtime.RuntimeArchitecture, Startup.Runtime.RuntimeVersion);
            //      Console.WriteLine("System: {0} {1}", Startup.Runtime.OperatingSystem, Startup.Runtime.OperatingSystemVersion);


            Console.WriteLine("Usage:");
            Console.WriteLine();
            IEnumerable<Common.ArgumentDescriptor> args = new Common.ArgumentDescriptor[]
            {
                new Common.ArgumentDescriptor() {Argument = "dnx run ", PostArguments = "<command> [<arguments....>]"}
            };

            Console.WriteLine(FormatArgumentDescriptors(args, Startup.DisplayWidth));
            Console.WriteLine("Commands:");
            Console.WriteLine();
            args = new Common.ArgumentDescriptor[]
            {
                new Common.ArgumentDescriptor()
                {
                    Argument = "/list",
                    Description = "Displays a list of tasks available."
                },
                new Common.ArgumentDescriptor() {Argument = "/help", Description = "Displays this help text."},
                new Common.ArgumentDescriptor()
                {
                    Argument = "/help",
                    PostArguments = "<task code>",
                    Description =
                        "Displays help text for the specific task including arguments usage and scheduling details."
                },
                new Common.ArgumentDescriptor()
                {
                    Argument = "/run",
                    PostArguments = "<task code> [arguments]",
                    Description = "Runs a specific task given its identifier code with optional arguments for the task."
                },
            };
            Console.WriteLine(FormatArgumentDescriptors(args, Startup.DisplayWidth));
            Console.WriteLine("Purpose:");
            Console.WriteLine();
            Console.WriteLine("  " + string.Join(
                Environment.NewLine + "  ",
                ("This tool is intended to be used as a Windows Scheduled Task. The scheduled " +
                 "task command line should use a /run command with a specific task code to run. " +
                 "The other commands are intended for off-line usage to help the administrator " +
                 "configure the scheduled tasks.").WordWrap(Startup.DisplayWidth - 3).ToArray()
                ));
            Console.WriteLine();
        }


        private static Startup _startup;

        public static void Main(string[] args)
        {
            //var barcodeData1 = GetInternalBarCode();
            //var barcodeData2 = GenerateRandomString(7);
            //GetNumber(12);
            //Console.WriteLine("barcode data 1 :: " + barcodeData1);
            //Console.WriteLine("barcode data 2 :: " + barcodeData2);
            //Console.ReadLine();
            //	var wineMadeEasy = new WineMadeEasyCatalog();
            //    wineMadeEasy.Run();
            //var digitEyes = new DigitEyes();
            //digitEyes.Run();
            //   var upcCatalog = new Task.UpcDb.Tasks.Catalog();
            //  upcCatalog.Run();

            //  var upcdb = new UpcDb.Tasks.Import();
            //    upcdb.Run();


            //          //  var deps = DependencyContext.Default;
            ////
            //            // var runtime = PlatformServices.Default;
            //            //   var env = PlatformServices.Default.Application;
            //            //     SandBox();

            //                return;
            //            try
            //            {

            // No arguments displays the usage text:
            if (args.Length == 0)
            {
                DisplayUsageText();
                Environment.ExitCode = -1;
                return;
            }

            _startup = new Startup();
            //Load the base application configuration
            _startup.Configure(args);

            //look and get packages that implements the IScheduleTask interface
            _startup.ConfigureServices();




            //get all instantiated task
            var registeredTaskInstances = _startup.GetServices<IScheduledTask>().ToList();

            //Validate all task codes
            var invalidTasks = registeredTaskInstances.Where(t => !t.TaskCode.IsIdentifier()).ToList();
            if (invalidTasks.Any())
            {
                foreach (var invalidTask in invalidTasks)
                {
                    //log the invalid task somethere
                }
                Environment.ExitCode = -1;
                return;
            }

            var diags = new DiagnosticReporting();
            // Parse the command line arguments:
            var argQueue = new Queue<string>(args);
            while (argQueue.Count > 0)
            {
                var arg = argQueue.Dequeue();
                if (arg.CaseInsensitiveEquals("/help"))
                {
                    // If no arguments provided after /help, display normal usage text:
                    if (argQueue.Count == 0)
                    {
                        DisplayUsageText();
                        Environment.ExitCode = -1;
                        return;
                    }

                    // Assume the next argument is the task code and find the specific task:
                    var code = argQueue.Dequeue();

                    var specTask =
                        registeredTaskInstances
                            .SingleOrDefault(task => task.TaskCode.CaseInsensitiveTrimmedEquals(code));
                    if (specTask == null)
                    {
                        Console.Error.WriteLine($"Could not find task for code \"{code}\"");
                        Environment.ExitCode = -1;
                        return;
                    }

                    // Display usage and scheduling information:
                    Console.WriteLine($"Task Code:   \"{specTask.TaskCode}\"");
                    Console.WriteLine("Description: {0}", string.Join(
                        Environment.NewLine + "             ",
                        specTask.TaskName.WordWrap(Startup.DisplayWidth - 14).ToArray()
                        ));
                    Console.WriteLine();
                    Console.WriteLine("Business Purpose:");
                    Console.WriteLine();
                    Console.WriteLine("  " + string.Join(
                        Environment.NewLine + "  ",
                        specTask.TaskDescription.WordWrap(Startup.DisplayWidth - 3).ToArray()
                        ));
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine();
                    Console.WriteLine(FormatArgumentDescriptors(specTask.ArgumentDescriptors, Startup.DisplayWidth));
                    Console.WriteLine("Scheduling:");
                    Console.WriteLine();

                    //TODO: Is it meaningful to display the scheduled of the task?
                    //    Console.WriteLine(FormatArgumentDescriptors(specTask.ScheduleTriggers, Startup.DisplayWidth));
                    //Console.WriteLine();
                    return;
                }
                if (arg.CaseInsensitiveEquals("/list"))
                {
                    // Lists all available tasks:
                    Console.WriteLine("Available tasks:");
                    Console.WriteLine();
                    foreach (var task in registeredTaskInstances.OrderBy(t => t.TaskCode))
                    {
                        Console.WriteLine($"  Task Code:   \"{task.TaskCode}\"");
                        Console.WriteLine("  Description: {0}", string.Join(
                            Environment.NewLine + "               ",
                            task.TaskName.WordWrap(Startup.DisplayWidth - 16).ToArray()
                            ));
                        Console.WriteLine();
                    }
                    return;
                }
                if (arg.CaseInsensitiveEquals("/run"))
                {


                    // Assume the next argument is the task code:
                    var code = argQueue.Dequeue();

                    // Find the specific task:
                    var specificTask =
                        registeredTaskInstances
                            .SingleOrDefault(task => task.TaskCode.CaseInsensitiveTrimmedEquals(code));
                    if (specificTask == null)
                    {
                        Console.Error.WriteLine("Could not find task for code '{0}'", code);
                        Environment.ExitCode = -1;
                        return;
                    }

                    string logCategory = "Test";
                    diags.Log(logCategory, "Selected task by task code '{0}': {1}", specificTask.TaskCode,
                        specificTask.TaskName);

                    using (specificTask)
                    {
                        Mutex mutex = null;
                        try
                        {
                            // Check the mutually-exclusive nature of the task on this machine:
                            if (!specificTask.AllowMultipleInstances)
                            {
                                // We have to create a named Mutex to be visible across processes on the machine, if it does not
                                // exist already:

                                bool createdNewMutex;
                                mutex = new Mutex(true, @"Global\ScheduleTask_" + specificTask.TaskCode,
                                    out createdNewMutex);
                                if (!createdNewMutex)
                                {
                                    diags.Log(logCategory,
                                        "Preventing task '{0}' from running multiple instances because AllowMultipleInstances was set to false.",
                                        specificTask.TaskCode);
                                    Environment.ExitCode = -2;
                                    return;
                                }
                            }

                            // Provide the dependency structure to the task:
                         //   specificTask.ProvideDependencies(new TaskDependencies(diags));

                            // Stuff the remaining arguments into a string[] and have the task parse its arguments:

                            diags.Log(logCategory, "Parsing arguments...");
                            bool success = specificTask.ParseArguments(argQueue.ToArray());
                            if (!success)
                            {
                                diags.Log(logCategory, "Parsing arguments has failed. Terminating.");
                                Environment.ExitCode = -2;
                                return;
                            }
                            diags.Log(logCategory, "Parsing arguments successfully completed.");

                            // Run the task:
                            diags.Log(logCategory, "Running task...");

                            success = specificTask.Run();

                            if (!success)
                            {
                                diags.Log(logCategory, specificTask.ErrorMessage);
                                diags.Log(logCategory, "Running task has failed. Terminating.");
                                Environment.ExitCode = -3;
                                return;
                            }
                        }
                        finally
                        {
                            mutex?.Close();
                        }
                    }

                    // Successful exit:
                    diags.Log(logCategory, "Running task successfully completed.");
                    Environment.ExitCode = 0;
                    return;
                }

                DisplayUsageText();
                Environment.ExitCode = -1;
            }

        }
    }
}




