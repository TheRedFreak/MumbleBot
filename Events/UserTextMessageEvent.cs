using System.Collections.Generic;
using System.Linq;
using MumbleBot.Types;
using MurmurRPC;

namespace MumbleBot
{
    public class UserTextMessageEvent
    {
        private V1.V1Client _client;
        public MumbleUser User { get; }
        public List<MumbleChannel> Channels { get; }
        public TextMessage Message { get; }
        public MumbleServer Server { get; }

        public UserTextMessageEvent(User user, List<Channel> channels, TextMessage message, Server server,
            V1.V1Client client)
        {
            _client = client;
            User = new MumbleUser(user, client);
            Channels = channels.Select(x => new MumbleChannel(x, _client)).ToList();
            Message = message;
            Server = new MumbleServer(server, client);
        }
    }
}