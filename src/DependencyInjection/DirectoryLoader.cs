using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace DependencyInjection
{
    public class DirectoryLoader 
    {

        private readonly string _path;

        public DirectoryLoader(string path)
        {
            _path = path;
            var loadableAssemblies = new List<Assembly>();

            var deps = DependencyContext.Default;
            foreach (var compilationLibrary in deps.CompileLibraries)
            {
                if (compilationLibrary.Name.Contains(""))
                {
                    var assembly = Assembly.Load(new AssemblyName(compilationLibrary.Name));
                    loadableAssemblies.Add(assembly);
                }
            }
        }

        public Assembly Load(AssemblyName assemblyName)
        {
            throw new NotImplementedException();
            //     return _context.LoadFile(Path.Combine(_path, assemblyName.Name + ".dll"));
        }

        public Assembly Load(string name)
        {
            throw new NotImplementedException();   //  return _context.Load(name);
        }
        public IntPtr LoadUnmanagedLibrary(string name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<AssemblyName> GetAssembliesReferencingThis(List<string> assemblyNames)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<AssemblyName> GetAssembliesReferencingThis(AssemblyName assemblyName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Assembly> CandidateAssemblies { get; }
    }
}