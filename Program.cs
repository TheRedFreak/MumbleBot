using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using log4net;
using MumbleBot.Plugins;

namespace MumbleBot
{
    public class Program
    {
        // public static Mumble MumbleInstance { get; private set; }

        // internal static event EventHandler StopRequested;
        // internal static string ExePath;
        internal static string WorkDir;

        private static bool stopRequested = false;

        private static IEnumerable<IPlugin> _plugins;

        internal static ILog logger { get; private set; }

        static void Main(string[] args)
        {
            Thread.CurrentThread.Name = "MainThread";

            // ExePath = Assembly.GetEntryAssembly()?.Location;

            WorkDir = Directory.GetCurrentDirectory();

            logger = LogManager.GetLogger("Main");

            // if (ExePath == null)
            // {
            //     logger.Fatal("Unable to retrieve path of execution!");
            //     Environment.Exit(-1);
            // }

            if (WorkDir == null)
            {
                logger.Fatal("Unable to determine WorkDir!");
                Environment.Exit(-1);
            }

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => { RequestStop(); };

            logger.Info($"Workdir: {WorkDir}");


            logger.Info("Initializing mumble gRPC client...");
            Mumble.Instance = new Mumble("http://192.168.2.2:50051");
            logger.Info("Client initialized.");


            logger.Info("Loading plugins...");

            if (!Directory.Exists(Path.Combine(WorkDir, "plugins")))
                Directory.CreateDirectory(Path.Combine(WorkDir, "plugins"));

            logger.Debug($"Plugins folder: {Path.Combine(WorkDir, "plugins")}");


            var pluginConfigFilesPath = Directory.GetFiles(Path.Combine(WorkDir, "plugins"), "*Plugin.json");
            var list = new List<String>();
            foreach (var configPath in pluginConfigFilesPath)
            {
                logger.Debug($"Parsing {configPath}");

                var config = JsonSerializer.Deserialize<PluginConfig>(File.ReadAllText(configPath));

                logger.Info($"Loading plugin {config.Name} v{config.Version}");

                if (!(config.Libs == "false" || config.Libs == ""))
                {
                    var libPaths = Directory.GetFiles(Path.Combine(WorkDir, "plugins", config.Libs), "*.dll");

                    logger.Debug($"{config.Name} has {libPaths.Length} libs to load. Loading them!");
                    foreach (string libPath in libPaths)
                    {
                        var loadCOntext = new PluginLibLoadContext(configPath);
                        var lib = loadCOntext.LoadFromAssemblyPath(libPath);
                        logger.Debug($"{lib.FullName} loaded.");
                    }

                    logger.Info($"Loaded {libPaths.Length} libs from {config.Name}!");
                }

                // logger.Info($"Adding {Path.GetFileName(Path.Combine(WorkDir, "plugins", config.Name + ".dll"))}");
                list.Add(Path.Combine(WorkDir, "plugins", config.Name + ".dll"));
            }

            var pluginPaths = list.ToArray();


            if (pluginPaths.Length == 0)
            {
                logger.Info("No plugins found...");
                _plugins = new List<IPlugin>();
            }
            else
            {
                logger.Info("Creating plugins...");
                _plugins = pluginPaths.SelectMany(pluginPath =>
                {
                    logger.Debug($"Loading {pluginPath}");
                    Assembly pluginAssembly = PluginLoader.LoadPlugin(pluginPath);

                    var config =
                        JsonSerializer.Deserialize<PluginConfig>(
                            File.ReadAllText(pluginPath.Replace(".dll", "Plugin.json")));

                    logger.Info($"Creating plugin {config.Name} v{config.Version}");

                    try
                    {
                        return PluginLoader.CreatePlugins(pluginAssembly);
                    }
                    catch
                    {
                        logger.Error($"Unable create plugin {pluginAssembly.FullName}");
                        return null;
                    }
                }).ToList();

                foreach (var plugin in _plugins)
                {
                    logger.Info($"Initializing plugin {plugin.Name} v*{plugin.Version}");
                    var pluginWorkDir = Path.Combine(WorkDir, "plugins", plugin.Name);
                    Directory.CreateDirectory(pluginWorkDir);
                    plugin?.Load();
                }
            }



            logger.Info("Starting gRPC client...");
            Mumble.Instance.Start();
            logger.Info("Client started.");

            logger.Info("Starting plugins...");

            foreach (var plugin in _plugins)
            {
                logger.Info($"Starting {plugin.Name} v{plugin.Version}");
                plugin?.Start();
            }

            logger.Info("Plugins started!");

            while (!stopRequested)
            {
                Thread.Sleep(200); // Because it should not exit!
            }


            DoShutdown();
        }

        public static void RequestStop()
        {
            logger.Info("Stop requested! Starting shutdown...");
            stopRequested = true;
        }

        private static void DoShutdown()
        {
            logger.Info("Disabling plugins...");

            foreach (var plugin in _plugins)
            {
                try
                {
                    plugin?.Stop();
                }
                catch (Exception e)
                {
                    if (plugin != null)
                    {
                        logger.Error(e);
                        logger.Error($"Exception caught while disabling Plugin {plugin.Name}");
                    }
                    else
                    {
                        logger.Error(e);
                        logger.Error("A entry in the pluginlist was null. You can probably ignore this.");
                    }
                }
            }

            logger.Info("Thank you! :)");
            Environment.Exit(0);
        }
    }
}