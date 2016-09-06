using System;
using System.Collections.Generic;

namespace Task.Common
{
    public interface IScheduledTask : IDisposable
    {
        /// <summary>
        /// Gets a short alphanumeric, whitespace-free code to uniquely identify this task.
        /// </summary>
        /// <remarks>
        /// This is validated on each start of the application. It must conform to the C# rules
        /// for an identifier. Must start with an alpha or underscore character, and continue with
        /// alphanumeric or underscore characters.
        /// </remarks>
        string TaskCode { get; }

        /// <summary>
        /// Gets the task name for display in a list.
        /// </summary>
        string TaskName { get; }

        /// <summary>
        /// When the app is run in /help mode, this text will be displayed to the user to describe
        /// the business purpose of the task.
        /// </summary>
        string TaskDescription { get; }

        /// <summary>
        /// Gets a flag which indicates whether or not multiple instances of the program running
        /// this task are allowed to run on the same machine at the same time.
        /// </summary>
        /// <remarks>
        /// <para>Note that this does not prevent multiple executions of this task across multiple machines;
        /// it is only effective within the context of a single machine. This flag is specific to
        /// this task only. This flag should have no effect on any other task separate from this one.</para>
        /// <para>This flag is used to allow cases where Windows Task Scheduler is set up to run TaskExecutor
        /// with very short intervals (e.g. every 5 minutes), but to only allow one process to run
        /// to completion. The other processes started while the first one is still running must
        /// exit if this flag is set to false.</para>
        /// </remarks>
        bool AllowMultipleInstances { get; }

        /// <summary>
        /// When the app is run in /help mode, this set of argument descriptors will be displayed
        /// to the user to describe the usage of commandline arguments.
        /// </summary>
        IEnumerable<ArgumentDescriptor> ArgumentDescriptors { get; }

        /// <summary>
        /// Gets the preferred schedule triggers this task should run on.
        /// </summary>
        IEnumerable<SchedulingTrigger> ScheduleTriggers { get; }

        /// <summary>
        /// Provides a set of functionality providers for the task to use.
        /// </summary>
        /// <remarks>
        /// This method is invoked first before all others when the task is set up.
        /// </remarks>
        /// <param name="deps">A structure containing necessary functionality provider dependencies for the task</param>
        void ProvideDependencies(TaskDependencies deps);

        /// <summary>
        /// Parse the command-line arguments for the task. Returns a boolean indicating success or failure.
        /// Upon failure, the task will not be executed.
        /// </summary>
        /// <remarks>
        /// This method is invoked before Run to ensure that the task arguments are valid and to parse out whatever
        /// information is necessary for task execution. The implementation should store whatever private state is
        /// necessary for the Run method to make use of.
        /// </remarks>
        /// <param name="args"></param>
        /// <returns></returns>
        bool ParseArguments(string[] args);

        /// <summary>
        /// Runs the task and returns overall success or failure.
        /// </summary>
        /// <returns></returns>
        bool Run();

        /// <summary>
        /// Gets the last error message from an unsuccessful run.
        /// </summary>
        string ErrorMessage { get; }
    }
}