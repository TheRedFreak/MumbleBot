using MumbleBot.Types;
using MurmurRPC;

namespace MumbleBot
{
    public class UserConnectedEvent
    {
        private readonly V1.V1Client _client;
        public MumbleUser User { get; }
        public MumbleChannel Channel { get; }
        public MumbleServer Server { get; }

        public UserConnectedEvent(User user, Channel channel, Server server, V1.V1Client client)
        {
            _client = client;
            User = new MumbleUser(user, client);
            Channel = channel == null ? null : new MumbleChannel(channel, client);
            Server = new MumbleServer(server, client);
        }
    }
}