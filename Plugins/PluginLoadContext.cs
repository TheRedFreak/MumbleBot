using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace MumbleBot.Plugins
{
    class PluginLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;
        private string pluginPath;


        public PluginLoadContext(string pluginPath)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
            this.pluginPath = pluginPath;



            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);
        }

        Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            if (pluginPath == null) return null;

            string folderPath = Path.Combine(Path.GetDirectoryName(pluginPath),
                Path.GetFileName(pluginPath).Split(".")[0], "libs");
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            // Console.WriteLine($"[[[CUSTOM LoadFromFolder]]] Trying to load {assemblyPath}. {File.Exists(assemblyPath)}");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            // Console.WriteLine($"Loading {assemblyName} from {assemblyPath}");

            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            Console.WriteLine($"Loading {unmanagedDllName} from {libraryPath}");
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}