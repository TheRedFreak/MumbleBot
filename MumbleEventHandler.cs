using System;
using System.Linq;
using System.Threading;
using Google.Protobuf.Collections;
using Grpc.Core;
using log4net;
using MumbleBot.Events;
using MumbleBot.Types;
using MurmurRPC;
using Void = MurmurRPC.Void;

namespace MumbleBot
{
    public partial class Mumble
    {
        private static ILog eventLogger;
        public event EventHandler<UserConnectedEvent> UserConnected;
        public event EventHandler<UserDisconnectedEvent> UserDisconnected;
        public event EventHandler<UserStateChangedEvent> UserStateChanged;
        public event EventHandler<UserTextMessageEvent> UserTextMessage;
        public event EventHandler<ChannelCreatedEvent> ChannelCreated;
        public event EventHandler<ChannelRemovedEvent> ChannelRemoved;
        public event EventHandler<ChannelStateChangedEvent> ChannelStateChanged;
        public event EventHandler<ContextActionEvent> OnContextActionEvent;


        private void RunServerEventThread()
        {
            var mumbleServerEvents = new Thread(MumbleServerEventThread) {Name = "MumbleServerEventThread"};
            mumbleServerEvents.Start();
        }

        private void RunVServerEventThreads()
        {
            var servers = GetAllServers();

            foreach (var serv in servers)
            {
                var vServerEvents = new Thread(MumbleVServerEventThread) {Name = $"MumbleVServer{serv.Id}EventThread"};
                vServerEvents.Start(serv);

                // Todo Wtf? 
                // var vServerContextActionEvents = new Thread(MumbleVServerContextActionEventThread)
                //     {Name = $"MumbleVServer{serv.Id}ContextActionEventThread"};
                // vServerContextActionEvents.Start(serv);
            }
        }

        private async void MumbleServerEventThread()
        {
            eventLogger.Info("Server event thread started.");
            using (var events = _client.Events(new Void(), Metadata.Empty))
            {
                await foreach (var ev in events.ResponseStream.ReadAllAsync())
                {
                    switch (ev.Type)
                    {
                        case Event.Types.Type.ServerStopped:
                            break;
                        case Event.Types.Type.ServerStarted:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    eventLogger.Debug($"EVENT: {ev.Type} {ev.Server.Id}");
                }
            }

            eventLogger.Info("Server event thread stopped.");
        }

        private async void MumbleVServerContextActionEventThread(object param)
        {
            if (param is not MumbleServer)
            {
                eventLogger.Warn("Unable to start VServer ContextAction thread. Param is not MumbleServer!");
                return;
            }

            var server = _client.ServerGet(new Server
            {
                Id = ((MumbleServer) param).Id
            });


            eventLogger.Info($"Server ContextAction event thread for server {server.Id} started.");

            using (var events = _client.ContextActionEvents(new ContextAction
            {
                Server = server,
                Text = "test",
                Action = "test",
            }, Metadata.Empty))
            {
                if (events is null)
                {
                    eventLogger.Error($"Unable to get VServer {((MumbleServer) param).Id}'s ContextActionEventStream!");
                    return;
                }

                await foreach (var ev in events.ResponseStream.ReadAllAsync())
                {
                    OnContextAction(ev.Action, ev.Actor, ev.Channel, ev.Context, ev.Server, ev.Text, ev.User);

                    eventLogger.Debug($"CONTEXTEVENT: {ev.Action} {ev.Server.Id}");
                }
            }

            eventLogger.Info("Server ContextAction event thread stopped.");
        }

        private async void MumbleVServerEventThread(object param)
        {
            if (param is not MumbleServer)
            {
                eventLogger.Warn("Unable to start VServer Event thread. Param is not MumbleServer!");
                return;
            }

            var server = _client.ServerGet(new Server
            {
                Id = ((MumbleServer) param).Id
            });

            if (server is null)
            {
                eventLogger.Error($"Unable to get VServer {((MumbleServer) param).Id}!");
                return;
            }

            if (!server.Running)
            {
                eventLogger.Error($"Skipping server {((MumbleServer) param).Id} because it's not running!");
                return;
            }


            eventLogger.Info($"VServer event thread for server {server.Id} started.");
            using (var events = _client.ServerEvents(server, Metadata.Empty))
            {
                if (events?.ResponseStream is null)
                {
                    eventLogger.Error($"Unable to get VServer {((MumbleServer) param).Id}'s EventStream!");
                    return;
                }

                await foreach (var ev in events.ResponseStream.ReadAllAsync())
                {
                    switch (ev.Type)
                    {
                        case Server.Types.Event.Types.Type.UserConnected:
                            OnUserConnected(ev.User, ev.Channel, ev.Server);
                            break;
                        case Server.Types.Event.Types.Type.UserDisconnected:
                            OnUserDisconnected(ev.User, ev.Channel, ev.Server);
                            break;
                        case Server.Types.Event.Types.Type.UserStateChanged:
                            OnUserStateChanged(ev.User, ev.Server);
                            break;
                        case Server.Types.Event.Types.Type.UserTextMessage:
                            OnUserTextMessage(ev.User, ev.Message, ev.Message.Channels, ev.Server);
                            break;
                        case Server.Types.Event.Types.Type.ChannelCreated:
                            OnChannelCreated(ev.Channel, ev.User, ev.Server);
                            break;
                        case Server.Types.Event.Types.Type.ChannelRemoved:
                            OnChannelRemoved(ev.Channel, ev.User, ev.Server);
                            break;
                        case Server.Types.Event.Types.Type.ChannelStateChanged:
                            OnChannelStateChanged(ev.Channel, ev.Server);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    eventLogger.Debug($"EVENT: {ev.Type} {ev.Server.Id}");
                }
            }

            eventLogger.Info("VServer event thread stopped.");
        }

        protected virtual void OnUserConnected(User user, Channel channel, Server server)
        {
            UserConnected?.Invoke(this, new UserConnectedEvent(user, channel, server, _client));
        }

        protected virtual void OnUserDisconnected(User user, Channel channel, Server server)
        {
            UserDisconnected?.Invoke(this, new UserDisconnectedEvent(user, channel, server, _client));
        }

        protected virtual void OnUserStateChanged(User user, Server server)
        {
            UserStateChanged?.Invoke(this, new UserStateChangedEvent(user, server, _client));
        }

        protected virtual void OnUserTextMessage(User user, TextMessage message, RepeatedField<Channel> channel,
            Server server)
        {
            UserTextMessage?.Invoke(this, new UserTextMessageEvent(user, channel.ToList(), message, server, _client));
        }

        protected virtual void OnChannelRemoved(Channel channel, User user, Server server)
        {
            ChannelRemoved?.Invoke(this, new ChannelRemovedEvent(channel, user, server, _client));
        }

        protected virtual void OnChannelCreated(Channel channel, User user, Server server)
        {
            ChannelCreated?.Invoke(this, new ChannelCreatedEvent(channel, user, server, _client));
        }

        protected virtual void OnChannelStateChanged(Channel channel, Server server)
        {
            ChannelStateChanged?.Invoke(this, new ChannelStateChangedEvent(channel, server, _client));
        }

        protected virtual void OnContextAction(string action, User actor, Channel channel, uint context, Server server,
            string text, User user)
        {
            OnContextActionEvent?.Invoke(this,
                new ContextActionEvent(action, text, actor, user, channel, context, server, _client));
        }
    }

    public class ContextActionEvent
    {
        private readonly string _action;
        private readonly string _text;
        private readonly MumbleUser _actor;
        private readonly MumbleUser _user;
        private readonly MumbleChannel _channel;
        private readonly uint _context;
        private readonly MumbleServer _server;

        public ContextActionEvent(string action, string text, User actor, User user, Channel channel, uint context,
            Server server, V1.V1Client client)
        {
            _action = action;
            _text = text;
            _actor = new MumbleUser(actor, client);
            _user = new MumbleUser(user, client);
            _channel = new MumbleChannel(channel, client);
            _context = context;
            _server = new MumbleServer(server, client);
        }


        public string Action => _action;

        public string Text => _text;

        public MumbleUser Actor => _actor;

        public MumbleUser User => _user;

        public MumbleChannel Channel => _channel;

        public uint Context => _context;

        public MumbleServer Server => _server;
    }
}