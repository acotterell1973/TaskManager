using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Task.PlugInsLoader
{
    public class DirectoryLoader : IAssemblyLoader
    {

        private readonly IAssemblyLoadContext _context;
        private readonly DirectoryInfo _path;

        public DirectoryLoader(DirectoryInfo path, IAssemblyLoadContext context)
        {
            _path = path;
            _context = context;
        }
        public DirectoryLoader(string path)
        {
            _path = new DirectoryInfo(path) ;
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
            return _context.LoadFile(Path.Combine(_path.FullName, assemblyName.Name + ".dll"));
        }

        public Assembly Load(string name)
        {
            return _context.Load(name);
        }
        public IntPtr LoadUnmanagedLibrary(string name)
        {
            throw new NotImplementedException();
        }

        //public IEnumerable<AssemblyName> GetAssembliesReferencingThis(List<string> assemblyNames)
        //{
        //    throw new NotImplementedException();
        //}

        //public IEnumerable<AssemblyName> GetAssembliesReferencingThis(AssemblyName assemblyName)
        //{
        //    throw new NotImplementedException();
        //}

        //public IEnumerable<Assembly> CandidateAssemblies { get; }
    }
}