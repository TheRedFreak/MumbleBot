using System;
using Grpc.Core;
using MumbleBot.Types;
using MurmurRPC;

namespace MumbleBot.ContextAction
{
    public class MumbleContextAction
    {
        private readonly V1.V1Client _client;

        internal MumbleUser User;

        public MumbleContextAction(string action, string text, MumbleServer server, MumbleChannel channel,
            MumbleUser user, ContextActionContext context, V1.V1Client client)
        {
            User = user;
            _client = client;
            Action = action;
            Text = text;
            Server = server;
            Channel = channel;
            Context = context;

            Init();
        }

        internal string Action { get; }
        internal string Text { get; }
        internal MumbleServer Server { get; }
        internal MumbleChannel Channel { get; }
        internal ContextActionContext Context { get; }

        public event EventHandler<MumbleContextActionEvent> Trigger;

        public async void Init()
        {
            using var events = _client.ContextActionEvents(new MurmurRPC.ContextAction
            {
                Action = Action,
                Channel = Channel?.GetMumbleChannel(),
                Context = (uint) Context,
                Server = Server?.GetMumbleServer(),
                Text = Text,
                User = User?.GetMumbleUser()
            });

            if (events?.ResponseStream is null) return;

            await foreach (var ev in events.ResponseStream.ReadAllAsync())
            {
                var ch = ev.Channel == null ? null : _client.ChannelGet(ev.Channel);
                var se = ev.Server == null ? null : _client.ServerGet(ev.Server);
                var us = ev.User == null ? null : _client.UserGet(ev.User);

                Trigger?.Invoke(this,
                    new MumbleContextActionEvent(ev.Action, ev.Text, ev.Context, ev.Actor, us, ch, se));
            }
        }
    }

    public class MumbleContextActionEvent
    {
        public MumbleContextActionEvent(string action, string text, uint context, User actor, User user,
            Channel channel, Server server)
        {
            Action = action;
            Actor = actor;
            Channel = channel;
            Context = context;
            Server = server;
            Text = text;
            User = user;
        }

        public string Action { get; }
        public User Actor { get; }
        public Channel Channel { get; }
        public uint Context { get; }
        public Server Server { get; }
        public string Text { get; }
        public User User { get; }
    }
}