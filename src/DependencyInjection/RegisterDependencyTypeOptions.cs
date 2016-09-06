using System.Collections.Generic;

namespace DependencyInjection
{
    public class RegisterDependencyTypeOptions
    {
        public RegisterDependencyTypeOptions()
        {
            QueueConnectionString = string.Empty;
            AssemblyPathLocation = string.Empty;
            InjectFromInterfaceName = string.Empty;
            TaskManifest = new List<string>();
        }

        public string QueueConnectionString { get; set; }
        public string AssemblyPathLocation { get; set; }
        public string InjectFromInterfaceName { get; set; }

        public List<string> TaskManifest { get; set; } 
    }
}