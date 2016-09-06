using System;

namespace Task.Common
{
    public interface IDiagnosticReporting
    {
        /// <summary>
        /// Log an exception.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="ex"></param>
        void LogException(string category, Exception ex);

        /// <summary>
        /// Log a line of diagnostic text.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="arg0"></param>
        void Log(string category, string arg0);

        /// <summary>
        /// Log a formatted line of diagnostic text.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void Log(string category, string format, params object[] args);
    }
}