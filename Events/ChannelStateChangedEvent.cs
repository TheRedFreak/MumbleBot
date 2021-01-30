using MurmurRPC;

namespace MumbleBot
{
    public class ChannelStateChangedEvent
    {
        private readonly V1.V1Client _client;
        public Channel Channel { get; }
        public Server Server { get; }

        public ChannelStateChangedEvent(Channel channel, Server server, V1.V1Client client)
        {
            _client = client;
            Channel = channel;
            Server = server;
        }
    }
}