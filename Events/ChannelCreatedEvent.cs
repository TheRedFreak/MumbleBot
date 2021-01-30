using MurmurRPC;

namespace MumbleBot
{
    public class ChannelCreatedEvent
    {
        private readonly V1.V1Client _client;
        public Channel Channel { get; }
        public User User { get; }
        public Server Server { get; }

        public ChannelCreatedEvent(Channel channel, User user, Server server, V1.V1Client client)
        {
            _client = client;
            Channel = channel;
            User = user;
            Server = server;
        }
    }
}