using System;
using MumbleBot.Types;
using MurmurRPC;

namespace MumbleBot.Events
{
    public class UserStateChangedEvent : EventArgs
    {
        private readonly V1.V1Client _client;
        public MumbleUser User { get; }
        public MumbleServer Server { get; }

        public UserStateChangedEvent(User user, Server server, V1.V1Client client)
        {
            _client = client;
            User = new MumbleUser(user, client);
            Server = new MumbleServer(server, client);
        }
    }
}