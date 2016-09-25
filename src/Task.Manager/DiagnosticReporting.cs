using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
//using Task.Common;

namespace Task.Manager
{
    internal class DiagnosticReporting// : IDiagnosticReporting
    {
        private TelemetryClient _telemetryClient;


        public DiagnosticReporting()
        {
            var configuration = TelemetryConfiguration.CreateDefault();
         //   configuration.InstrumentationKey = ConfigurationManager.AppSettings["InstrumentationKey"];
            _telemetryClient = new TelemetryClient(configuration);
        }

        #region IDiagnosticReporting Members

        public void LogException(string category, Exception applicationError)
        {
            Console.Error.WriteLine(applicationError.ToString());

            var properties = new Dictionary<string, string>
            {
                { "Application", category },
                { "tentantName", "" },
                { "Message",  applicationError.Message }
            };
            var metrics = new Dictionary<string, double> { { "count", 0 } };
            _telemetryClient.TrackException(applicationError, properties, metrics);
        }

        public void Log(string category, string arg0)
        {
            var properties = new Dictionary<string, string>
            {
                { "Application", category },
                { "tentantName", "" }
            };
            var metrics = new Dictionary<string, double> { { "count", 0 } };
            _telemetryClient.TrackEvent(category,properties,metrics);
            Console.WriteLine(arg0);
        }

        public void Log(string category, string format, params object[] args)
        {
   
            Console.WriteLine(format, args);
        }

        #endregion

    }
}