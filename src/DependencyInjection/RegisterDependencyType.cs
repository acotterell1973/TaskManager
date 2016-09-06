using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.PlatformAbstractions;

namespace DependencyInjection
{
    public class RegisterDependencyType
    {
        private readonly DirectoryLoader _loader;
        private readonly AssemblyName _thisAssemblyName;
        private readonly RegisterDependencyTypeOptions _options;
        public RegisterDependencyType(IOptions<RegisterDependencyTypeOptions> options)
        {
            
            _options = options.Value;
            _loader = new DirectoryLoader(_options.AssemblyPathLocation);
            _thisAssemblyName = new AssemblyName(GetType().GetTypeInfo().Assembly.FullName);
        }

        public void RegisterAllAssemblies(IServiceCollection services)
        {
            var allLibraries = _loader.GetAssembliesReferencingThis(_options.TaskManifest);
            foreach (var assembly in allLibraries)
            {
                RegisterAssembly(services, assembly);
            }
        }

        public void RegisterAssembly(IServiceCollection services, string assemblyName)
        {
            RegisterAssembly( services, new AssemblyName(assemblyName));
        }

        public void RegisterAssembly(IServiceCollection services, AssemblyName assemblyName)
        {

            var assembly = _loader.Load(assemblyName.Name);
         
            foreach (var type in assembly.DefinedTypes)
            {
                var taskInterface = type.GetInterface(_options.InjectFromInterfaceName, true);
                if (taskInterface == null) continue;

                var dependencyAttributes = type.GetCustomAttributes<Attributes.DependencyAttribute>();
               
                // Each dependency can be registered as various types
                foreach (var serviceDescriptor in dependencyAttributes.Select(dependencyAttribute => dependencyAttribute.BuiildServiceDescriptor(type)))
                {
                    services.Add(serviceDescriptor);
                }
            }
        }
    }
}
