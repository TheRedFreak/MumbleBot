using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using NuGet;

namespace MumbleBot.Plugins
{
    class PluginLibLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;


        public PluginLibLoadContext(string pluginPath)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            // Console.WriteLine($"Loading {assemblyName} from {assemblyPath}");

            if (assemblyPath != null || assemblyName.Name.StartsWith("System."))
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
            else
            {
                Console.WriteLine($"Trying to download {assemblyName.Name}@{assemblyName.Version}");
                Debug.Assert(assemblyName.Version != null, "assemblyName.Version != null");
                DownloadDependency(assemblyName.Name, assemblyName.Version.ToString());
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

        private bool inited = false;
        private IPackageRepository repo;
        private PackageManager packageManager;

        private void DownloadDependency(string name, string ver)
        {
            if (!inited)
            {
                repo = PackageRepositoryFactory.Default
                    .CreateRepository("https://packages.nuget.org/api/v2");

                string path = ".\\dependencies";
                packageManager = new PackageManager(repo, path);
                packageManager.PackageInstalled += PackageManager_PackageInstalled;
            }

            var package = repo.FindPackage(name, SemanticVersion.Parse(ver));
            if (package != null)
            {
                packageManager.InstallPackage(package, false, true);
            }
        }

        private void PackageManager_PackageInstalled(object sender, PackageOperationEventArgs args)
        {
            Console.WriteLine($"DEPS: Installed! {args.Package.Title} {args.Package.Version.Version} ");
        }
    }
}