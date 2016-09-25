using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.PlatformAbstractions;

namespace Task.PlugInsLoader
{
    public class RegisterDependencyTypeOptions
    {
        public string AssemblyPathLocation { get; set; }
        public string InjectFromInterfaceName { get; set; }
        public IFileProvider FileProvider { get; set; }
        public IAssemblyLoadContextAccessor LoadContextAccessor { get; set; }
        public IAssemblyLoaderContainer AssemblyLoaderContainer { get; set; }
    }
}