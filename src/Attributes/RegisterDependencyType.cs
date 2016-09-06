using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;

namespace Attributes
{
    public class RegisterDependencyType
    {
        public void RegisterAssembly(IServiceCollection services, AssemblyName assemblyName, string path = "")
        {
            var loadContext = PlatformServices.Default.AssemblyLoadContextAccessor.Default;
            var loader = new DirectoryLoader(path, loadContext);

            var assembly = loader.Load(assemblyName);
            foreach (var type in assembly.DefinedTypes)
            {
                var dependencyAttributes = type.GetCustomAttributes<DependencyAttribute>();
                // Each dependency can be registered as various types
                foreach (var serviceDescriptor in dependencyAttributes.Select(dependencyAttribute => dependencyAttribute.BuiildServiceDescriptor(type)))
                {
                    services.Add(serviceDescriptor);
                }
            }
        }
    }
}
