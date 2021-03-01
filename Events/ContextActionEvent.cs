using MumbleBot.Types;
using MurmurRPC;

namespace MumbleBot
{
    public class ContextActionEvent
    {
        public ContextActionEvent(string action, string text, User actor, User user, Channel channel, uint context,
            Server server, V1.V1Client client)
        {
            Action = action;
            Text = text;
            Actor = new MumbleUser(actor, client);
            User = new MumbleUser(user, client);
            Channel = new MumbleChannel(channel, client);
            Context = context;
            Server = new MumbleServer(server, client);
        }


        public string Action { get; }

        public string Text { get; }

        public MumbleUser Actor { get; }

        public MumbleUser User { get; }

        public MumbleChannel Channel { get; }

        public uint Context { get; }

        public MumbleServer Server { get; }
    }
}