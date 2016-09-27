using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Configuration;

using Task.Manager.config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.PlatformAbstractions;
using Task.PlugInsLoader;
using Task.PlugInsLoader.Extensions;
#pragma warning disable 649

namespace Task.Manager
{

    public class Startup
    {
        private static IConfigurationRoot _configuration;
        private static IServiceProvider _provider;
        private readonly IConfigurationBuilder _configurationBuilder;
        private readonly IServiceCollection _services;
   

        public Startup()
        {
            _configurationBuilder = new ConfigurationBuilder();
            _services = new ServiceCollection();
        }
        public static ApplicationEnvironment Application => PlatformServices.Default.Application;

        public static int DisplayWidth { get; set; } = 80;

        public void Configure(string[] args)
        {
            _configurationBuilder
                .SetBasePath(Application.ApplicationBasePath)
                .AddJsonFile($@"config\development.json");
            _configuration = _configurationBuilder.Build();


        }

        public void ConfigureServices()
        {
            var fileProvider = new PhysicalFileProvider(_configuration.GetSection("ExternalAssemblyPath").Value);

            _services.AddOptions();  //Make IOptions available via D.I.
            _services.AddApplicationInsightsTelemetry(_configuration);

            //Make TaskManagerConfigurationSettings available via IOptions
            _services.Configure<TaskManagerConfigurationSettings>(_configuration.GetSection(string.Empty));
            _services.Configure<RegisterDependencyTypeOptions>(options =>
            {
                options.AssemblyPathLocation = _configuration.GetSection("externalAssemblyPath").Value;
                options.InjectFromInterfaceName = _configuration.GetSection("interfaceType").Value;
                options.FileProvider = fileProvider;
            });
            
            //  Custom Application Services
            _services
                .AddDependencyScanner() //Register Dependency Scan
                .AddDependencyScan();    //Scan Internal Library
               // .AddDependencyScanFromAllAssemblies();

            _provider = _services.BuildServiceProvider();
        }

        public T GetService<T>()
        {
            return _provider.GetService<T>();
        }

        public IEnumerable<T> GetServices<T>()
        {
            return _provider.GetServices<T>();
        }
    }
}
