using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;

namespace MumbleBot.Plugins
{
    class PluginLoader
    {
        public static Assembly LoadPlugin(string relativePath)
        {
            string pluginLocation = Path.GetFullPath(relativePath.Replace('\\', Path.DirectorySeparatorChar));
            PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }

        public static IEnumerable<IPlugin> CreatePlugins(Assembly assembly)
        {
            int count = 0;

            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type))
                {
                    var result = Activator.CreateInstance(type) as IPlugin;

                    if (result == null) continue;

                    result._mumble = Mumble.Instance;
                    result.logger = LogManager.GetLogger(result.Name);

                    count++;
                    yield return result;
                }
            }

            if (count == 0)
            {
                string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
                throw new ApplicationException(
                    $"Can't find any type which implements IPlugin in {assembly} from {assembly.Location}.\n" +
                    $"Available types: {availableTypes}");
            }
        }
    }
}