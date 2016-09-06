using System.Collections.Generic;

namespace Task.Manager.config
{
    public interface ITaskManagerConfigurationSettings
    {
        string QueueConnectionString { get; set; }
    
        string TaskLib { get; set; }
        string InterfaceType { get; set; }

        string ExternalAssemblyPath { get; set; }

        List<string> TaskManifest { get; set; } 

    }
}