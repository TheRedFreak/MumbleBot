using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MumbleBot
{
    public class BotConfig
    {
        public static Dictionary<string, string> cfgMap;

        public static void Load()
        {
            var path = Path.Combine(Program.WorkDir, "settings.json");

            if (!File.Exists(path)) File.WriteAllText(path, "{\"address\":\"http://127.0.0.1:50051\"}");

            var cfg = File.ReadAllText(path);
            try
            {
                cfgMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(cfg);
            }
            catch (Exception e)
            {
                Program.logger.Error(e);
                Program.logger.Error("Unable to load config!");
            }
        }

        public static void Save()
        {
            var path = Path.Combine(Program.WorkDir, "settings.json");

            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(cfgMap));
            }
            catch (Exception e)
            {
                Program.logger.Error(e);
                Program.logger.Error("Unable to save config!");
            }
        }
    }
}