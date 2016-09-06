//using System;
//using System.Collections.Generic;
//using DependencyInjection;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.OptionsModel;

//using Task.Manager.config;
//using DependencyInjection.Extensions;
//using Microsoft.Extensions.PlatformAbstractions;

//namespace Task.Manager
//{

//    public class Startup
//    {
//        private static IConfigurationRoot _configuration;
//        private static IServiceProvider _provider;
//        private readonly IConfigurationBuilder _configurationBuilder;
//        private readonly IServiceCollection _services;

//        public Startup()
//        {
//            _configurationBuilder = new ConfigurationBuilder();
//            _services = new ServiceCollection();
//        }

  
//        public static ApplicationEnvironment Application => PlatformServices.Default.Application;

//        public static TaskManagerConfigurationSettings ConfigurationSettings { get; private set; }
//        public static int DisplayWidth { get; set; }

//        public void Configure(string[] args)
//        {

//            var switchMappings = new Dictionary<string, string>
//            {
//                {"--help", "displayHelp" },
//                {"-h", "displayHelp" },
//            };

//            _configurationBuilder
//                .SetBasePath(Application.ApplicationBasePath)
//                .AddJsonFile($@"config\development.json");

//            determine if argument is in name/value pair (this is if you ever want to override any of the application settings)
//            if (false) _configurationBuilder.AddCommandLine(args, switchMappings);


//            _configuration = _configurationBuilder.Build();

//        }

//        public void ConfigureServices()
//        {
//            _services.AddOptions();
//            _services.Configure<TaskManagerConfigurationSettings>(_configuration.GetSection(string.Empty));

//            ConfigurationSettings = _configuration.Get<TaskManagerConfigurationSettings>();

//               _services.AddApplicationInsightsTelemetry(_configuration);
//            _services.AddInstance<IAssemblyLoadContext>(PlatformServices.Default.AssemblyLoadContextAccessor.Default);
//            _services.AddInstance<ILibraryManager>(PlatformServices.Default.LibraryManager);
//            _services.AddTransient<IConfigureOptions<RegisterDependencyTypeOptions>, RegisterDependencyTypeOptionsSetup>();
//            _services.Configure<RegisterDependencyTypeOptions>(options =>
//            {
//                options.QueueConnectionString = ConfigurationSettings.QueueConnectionString;
//                options.AssemblyPathLocation = ConfigurationSettings.ExternalAssemblyPath;
//                options.InjectFromInterfaceName = ConfigurationSettings.InterfaceType;
//                options.TaskManifest = ConfigurationSettings.TaskManifest;
//            });

//            Custom Application Services
//            _services.AddDependencyScanner()
//                .AddDependencyScan()
//                .AddDependencyScanFromAllAssemblies();

//            _provider = _services.BuildServiceProvider();



//        }

//        public T GetService<T>()
//        {
//            return _provider.GetService<T>();
//        }

//        public IEnumerable<T> GetServices<T>()
//        {
//            return _provider.GetServices<T>();
//        }
//    }
//}
