using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using log4net;
using MumbleBot.Plugins;

namespace MumbleBot
{
    public class Program
    {
        internal static string WorkDir;

        private static bool stopRequested;

        private static List<IPlugin> _plugins;

        internal static ILog logger { get; private set; }

        private static void Main(string[] args)
        {
            Thread.CurrentThread.Name = "MainThread";

            // This is for testing qodana

            AppDomain.CurrentDomain.ProcessExit += (_, _) => stopRequested = true; // Cannot cancel this event...
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => {
                e.Cancel = true;
                RequestStop();
            };

            WorkDir = Directory.GetCurrentDirectory();

            logger = LogManager.GetLogger("Main");

            if (WorkDir == null)
            {
                logger.Fatal("Unable to determine WorkDir!");
                Environment.Exit(-1);
            }

            logger.Info($"Workdir: {WorkDir}");
            logger.Info("Loading settings...");

            BotConfig.Load();


            logger.Info("Initializing mumble gRPC client...");

            var ip = BotConfig.cfgMap["address"];

            var uri = new Uri(ip);

            try
            {
                using var client = new TcpClient(uri.Host, uri.Port);
                logger.Info($"{ip} seems to be up. Connecting...");
            }
            catch (SocketException ex)
            {
                logger.Error($"Unable to ping host {ip}.");
                Environment.Exit(-1);
            }

            Mumble.Instance = new Mumble(ip);


            logger.Info("Client initialized.");


            logger.Info("Loading plugins...");

            if (!Directory.Exists(Path.Combine(WorkDir, "plugins")))
                Directory.CreateDirectory(Path.Combine(WorkDir, "plugins"));

            logger.Debug($"Plugins folder: {Path.Combine(WorkDir, "plugins")}");


            var pluginConfigFilesPath = Directory.GetFiles(Path.Combine(WorkDir, "plugins"), "*Plugin.json");
            var list = new List<string>();
            foreach (var configPath in pluginConfigFilesPath)
            {
                logger.Debug($"Parsing {configPath}");

                var config = JsonSerializer.Deserialize<PluginConfig>(File.ReadAllText(configPath));

                logger.Info($"Loading plugin {config.Name} v{config.Version}");

                if (!(config.Libs == "false" || config.Libs == ""))
                {
                    var libPaths = Directory.GetFiles(Path.Combine(WorkDir, "plugins", config.Libs), "*.dll");

                    logger.Debug($"{config.Name} has {libPaths.Length} libs to load. Loading them!");
                    foreach (var libPath in libPaths)
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
                    var pluginAssembly = PluginLoader.LoadPlugin(pluginPath);

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
                    logger.Info($"Initializing plugin {plugin.Name} v{plugin.Version}");
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

            while (!stopRequested) Thread.Sleep(200); // Because it should not exit!


            DoShutdown();
        }

        public static void RequestStop()
        {
            logger.Info("Stop requested! Starting shutdown...");
            stopRequested = true;
        }

        private static void DoShutdown()
        {
            logger.Info("Exiting...");
            logger.Info("Disabling plugins...");


            if (_plugins != null && _plugins.Count > 0)
            {
                logger.Info($"Disabling {_plugins.Count} plugins...");
                foreach (var plugin in _plugins)
                {
                    logger.Info($"Disabling plugin {plugin.Name}...");
                    plugin.Stop();
                    logger.Info($"Plugin {plugin.Name} disabled!");
                }
            }

            if (_plugins != null && _plugins.Count > 0)
                foreach (var plugin in _plugins)
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
                            logger.Error("A entry on the pluginlist was null. You can probably ignore this.");
                        }
                    }

            logger.Info("Goodbye, thank you! :)");
            Environment.Exit(0);
        }
    }
}
