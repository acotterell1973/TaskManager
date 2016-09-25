using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable InconsistentNaming

namespace Task.PlugInsLoader.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddDependencyScanner(this IServiceCollection services)
        {
            services.AddSingleton<RegisterDependencyType>();
            return services;
        }

        public static IServiceCollection AddDependencyScan(this IServiceCollection services)
        {
            var appEnv = PlatformServices.Default.Application; 
            services.ScanFromAssembly(new AssemblyName(appEnv.ApplicationName));
            return services;
        }

        public static IServiceCollection AddDependencyScanFromAllAssemblies(this IServiceCollection services)
        {
            var scanner = services.GetDependencyScanner();
            scanner.RegisterAllAssemblies(services);
            return services;
        }

        public static IServiceCollection ScanFromAssembly(this IServiceCollection services, AssemblyName assemblyName)
        {
            var scanner = services.GetDependencyScanner();
            scanner.RegisterAssembly(services, assemblyName);
            return services;
        }



        private static RegisterDependencyType GetDependencyScanner(this IServiceCollection services)
        {
            var scanner = services.BuildServiceProvider().GetService<RegisterDependencyType>();
            if (null == scanner)
            {
                throw new InvalidOperationException(
                    "Unable to resolve scanner. Call services.AddDependencyScanner");
            }
            return scanner;
        }


    }
}
