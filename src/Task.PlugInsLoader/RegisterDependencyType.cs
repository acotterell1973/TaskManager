using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;

namespace Task.PlugInsLoader
{
    public class RegisterDependencyType
    {
        private readonly DirectoryAssemblyProvider _externalDirectoryAssemblyProvider;
        private readonly DirectoryAssemblyProvider _internalDirectoryAssemblyProvider;
        private readonly AssemblyName _thisAssemblyName;
        private readonly RegisterDependencyTypeOptions _options;

        public RegisterDependencyType(IOptions<RegisterDependencyTypeOptions> options)
        {

            _options = options.Value;
            _externalDirectoryAssemblyProvider = new DirectoryAssemblyProvider(_options.AssemblyPathLocation, _options.InjectFromInterfaceName, _options.FileProvider);

            _internalDirectoryAssemblyProvider = new DirectoryAssemblyProvider(string.Empty, _options.InjectFromInterfaceName, _options.FileProvider);
            _thisAssemblyName = new AssemblyName(GetType().GetTypeInfo().Assembly.FullName);
        }

        public void RegisterAllAssemblies(IServiceCollection services)
        {
            //var allLibraries = _loader.GetAssembliesReferencingThis(_options.TaskManifest);
            //foreach (var assembly in allLibraries)
            //{
            //    RegisterAssembly(services, assembly);
            //}
        }

        public void RegisterAssembly(IServiceCollection services, string assemblyName)
        {
            RegisterAssembly(services, new AssemblyName(assemblyName));
        }

        public void RegisterAssembly(IServiceCollection services, AssemblyName assemblyName)
        {
            var availableTasks = _internalDirectoryAssemblyProvider.CandidateAssemblies;
            var availableTasksList = availableTasks.ToList();

            foreach (var type in availableTasksList)
            {
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
