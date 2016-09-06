using System.Collections.Generic;

namespace Task.Manager.config
{
    public class TaskManagerConfigurationSettings : ITaskManagerConfigurationSettings
    {
        public string QueueConnectionString { get; set; }
        public string TaskLib { get; set; }
        public string InterfaceType { get; set; }
        public string ExternalAssemblyPath { get; set; }
        public List<string> TaskManifest { get; set; }
    }
}
