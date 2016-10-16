using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Task.Common;

namespace Task.UpcDb
{
    public abstract class BaseTask : IScheduledTask
    {
        internal string _taskCode;
        private TaskDependencies _taskDependencies;

        protected BaseTask(string taskCode)
        {
            _taskCode = taskCode;
        }

        public virtual void Dispose()
        {
            
        }

        public virtual string TaskCode => _taskCode;
        public abstract string TaskName { get; }
        public abstract string TaskDescription { get; }
        public virtual bool AllowMultipleInstances { get; } = true;
        public virtual IEnumerable<ArgumentDescriptor> ArgumentDescriptors => new[]
        {
            new ArgumentDescriptor("/test", "Enables test mode which disables delivery of any notifications"),
            new ArgumentDescriptor("/date", "<mm/dd/yyyy>", "Overrides today's date for testing purposes")
        };

        public IEnumerable<SchedulingTrigger> ScheduleTriggers => Enumerable.Repeat(new SchedulingTrigger()
        {
            Daily = new SchedulingTrigger.DailyTrigger() { EveryNDays = 1 },
            RepeatTask = true,
            Every = new SchedulingTrigger.RepeatEvery() { HoursOrMinutes = HoursOrMinutes.Minutes, Number = 5 },
            StartDateTime = DateTime.Today.AddHours(9),
        }, 1);

        public void ProvideDependencies(TaskDependencies deps)
        {
            _taskDependencies = deps;
        }

        public virtual bool ParseArguments(string[] args)
        {
      

            var argQueue = new Queue<string>(args);
            while (argQueue.Count > 0)
            {
                var arg = argQueue.Dequeue();
                if (arg.CaseInsensitiveTrimmedEquals("/test"))
                {
                    // Enables test mode where orion tickets are simply output to the log.
                    EnableTestMode();
                }
                else if (arg.CaseInsensitiveTrimmedEquals("/date"))
                {
                    if (argQueue.Count == 0)
                    {
                        Log("Required <mm/dd/yyyy> argument after /date argument!");
                        return false;
                    }

                    var arg1 = argQueue.Dequeue();
                    DateTime tmpDate;

                    // Override the testable Today date with one passed from the command line:
                    if (DateTime.TryParse(arg1, out tmpDate))
                    {
                
                    }
                    else
                    {
                        Log("Could not parse <mm/dd/yyyy> argument after /date argument!");
                        return false;
                    }
                }
            }

            return true;
        }

        public abstract bool Run();
    
        public string ErrorMessage { get; protected set; }

        /// <summary>
        /// Log a line of text using the provided diagnostic reporting interface.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected internal virtual void Log(string format, params object[] args)
        {
 
        }

        /// <summary>
        /// Log a line of text using the provided diagnostic reporting interface.
        /// </summary>
        /// <param name="arg0"></param>
        protected internal virtual void Log(string arg0)
        {
  
        }

        /// <summary>
        /// Log an exception using the provided diagnostic reporting interface.
        /// </summary>
        /// <param name="ex"></param>
        protected internal virtual void LogException(Exception ex)
        {
    
        }

        /// <summary>
        /// Enables test mode which disables delivery of event notifications and Orion tickets.
        /// </summary>
        protected void EnableTestMode()
        {
       
        }
    }
}
