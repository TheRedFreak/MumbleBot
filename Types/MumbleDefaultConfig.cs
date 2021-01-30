using System.Collections.Generic;
using MurmurRPC;

namespace MumbleBot
{
    public class MumbleDefaultConfig
    {
        private Config Config { get; set; }
        private V1.V1Client Client { get; set; }

        private Server Server => Config.Server;

        public ICollection<string> Keys => Config.Fields.Keys;
        public ICollection<string> Values => Config.Fields.Values;
        public int Count => Config.Fields.Count;
        public IEnumerator<KeyValuePair<string, string>> Enumerator => Config.Fields.GetEnumerator();

        public MumbleDefaultConfig(Config config, V1.V1Client client)
        {
            Config = config;
            Client = client;
        }

        public string GetValue(string key)
        {
            return Config.Fields.ContainsKey(key) ? Config.Fields[key] : null;
        }

        public bool ContainsKey(string key)
        {
            return Config.Fields.ContainsKey(key);
        }
    }
}