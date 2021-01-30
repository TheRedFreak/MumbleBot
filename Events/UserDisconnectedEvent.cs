using MurmurRPC;

namespace MumbleBot
{
    public class UserDisconnectedEvent
    {
        private readonly V1.V1Client _client;
        public User User { get; }
        public Channel Channel { get; }
        public Server Server { get; }

        public UserDisconnectedEvent(User user, Channel channel, Server server, V1.V1Client client)
        {
            _client = client;
            User = user;
            Channel = channel;
            Server = server;
        }
    }
}