using Grpc.Core;
using MurmurRPC;

namespace MumbleBot.Types
{
    public class MumbleConfig : MumbleDefaultConfig
    {
        private Config Config { get; set; }
        private V1.V1Client Client { get; set; }

        private Server Server => Config.Server;

        public MumbleConfig(Config config, V1.V1Client client) : base(config, client)
        {
            Config = config;
            Client = client;
        }

        public string GetValue(string key)
        {
            Update();
            return Config.Fields.ContainsKey(key) ? Config.Fields[key] : null;
        }

        public bool ContainsKey(string key)
        {
            Update();
            return Config.Fields.ContainsKey(key);
        }

        public void SetValue(string key, string value)
        {
            Client.ConfigSetField(new Config.Types.Field
            {
                Key = key,
                Value = value,
                Server = Server
            });
            Update();
        }

        private void Update()
        {
            Config = Client.ConfigGet(Server, Metadata.Empty);
        }
    }
}