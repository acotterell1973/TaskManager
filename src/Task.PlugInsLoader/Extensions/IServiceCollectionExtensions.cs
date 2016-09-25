using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;

// ReSharper disable InconsistentNaming

namespace Task.PlugInsLoader.Extensions
{
    public static class IServiceCollectionExtensions
    {

        /// <summary>
        /// Registers the Dependency Task Scanner
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDependencyScanner(this IServiceCollection services)
        {
            services.AddSingleton<RegisterDependencyType>();
            return services;
        }

        /// <summary>
        /// Scans Internal Library to register internal Tasks 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDependencyScan(this IServiceCollection services)
        {
            var appEnv = PlatformServices.Default.Application;
            services.ScanFromAssembly(new AssemblyName(appEnv.ApplicationName));
            return services;
        }

        /// <summary>
        /// Scans external source for dependency Task from all assemblies.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDependencyScanFromAllAssemblies(this IServiceCollection services)
        {
            var scanner = services.GetDependencyScanner();
            scanner.RegisterAllAssemblies(services);
            return services;
        }

        #region Helpers

        /// <summary>
        /// Scans external source for dependency Task from the specificed assembly.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        public static IServiceCollection ScanFromAssembly(this IServiceCollection services, AssemblyName assemblyName)
        {
            var scanner = services.GetDependencyScanner();
            scanner.RegisterAssembly(services, assemblyName);
            return services;
        }


        /// <summary>
        /// Gets the Scanner Service
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
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

        #endregion
        
    }
}
