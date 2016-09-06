using System;

namespace Task.Common
{
    /// <summary>
    /// A structure which holds the necessary functionality dependencies for a task to use.
    /// </summary>
    public struct TaskDependencies
    {
        /// <summary>
        /// An interface responsible for diagnostic reporting, such as logging exceptions.
        /// </summary>
        public IDiagnosticReporting Diagnostics { get; }

        public TaskDependencies(IDiagnosticReporting diagnosticReporting)
        {
            if (diagnosticReporting == null) throw new ArgumentNullException("diagnosticReporting");

            Diagnostics = diagnosticReporting;
        }
    }
}