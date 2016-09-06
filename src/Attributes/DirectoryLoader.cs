using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.PlatformAbstractions;


namespace Attributes
{
    public class DirectoryLoader : IAssemblyLoader
    {
        private readonly IAssemblyLoadContext _context;
        private readonly string _path;

        public DirectoryLoader(string path, IAssemblyLoadContext context)
        {
            _path = path;
            _context = context;
        }

        public Assembly Load(AssemblyName assemblyName)
        {
            return _context.LoadFile(Path.Combine(_path, assemblyName.Name + ".dll"));
        }

        public Assembly Load(string name)
        {
            return _context.Load(name);
        }
        public IntPtr LoadUnmanagedLibrary(string name)
        {
            throw new NotImplementedException();
        }
    }
}