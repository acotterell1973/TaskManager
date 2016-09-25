using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace Task.PlugInsLoader
{
    public class DirectoryAssemblyProvider
    {
        private readonly IFileProvider _fileProvider;
        private readonly string _plugInsPath;
        private readonly string _interfaceName;


        public DirectoryAssemblyProvider(
                string plugInsPath,
                string Interface,
                IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
            _plugInsPath = plugInsPath;
            _interfaceName = Interface;
        }

        public IEnumerable<Assembly> CandidateAssemblies
        {
            get
            {
                var binFolder = string.Empty; //"bin";
                var content = _fileProvider.GetDirectoryContents(_plugInsPath);
                if (!content.Exists) yield break;
                foreach (var pluginDir in content.Where(x => x.IsDirectory))
                {
                    var binDir = new DirectoryInfo(Path.Combine(pluginDir.PhysicalPath, binFolder));
                    if (!binDir.Exists) continue;
                    foreach (var assembly in GetAssembliesInFolder(binDir))
                    {
                        yield return assembly;
                    }
                }
            }
        }

        /// <summary>
        /// Returns assemblies loaded from /bin folders inside of App_Plugins
        /// </summary>
        /// <param name="binPath"></param>
        /// <returns></returns>
        private IEnumerable<Assembly> GetAssembliesInFolder(DirectoryInfo binPath)
        {
            Assembly assembly = null;
            foreach (var fileSystemInfo in binPath.GetFileSystemInfos("*.dll"))
            {
                assembly = Assembly.Load(AssemblyName.GetAssemblyName(fileSystemInfo.FullName));
                try
                {
                    foreach (var taskInterface in assembly.DefinedTypes
                        .Select(definedType => definedType.GetInterface(_interfaceName, true))
                        .Where(taskInterface => taskInterface == null))
                    {
                    }

                }
                catch (Exception)
                {
                    // ignored
                }
            }
            yield return assembly;
        }
    }
}
