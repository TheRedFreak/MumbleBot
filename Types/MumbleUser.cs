using System;
using System.Linq;
using Grpc.Core;
using MurmurRPC;

namespace MumbleBot.Types
{
    public class MumbleUser
    {
        private readonly V1.V1Client _client;
        private readonly User _user;

        public MumbleUser(User user, V1.V1Client client)
        {
            _client = client;
            _user = _client.UserGet(user);
        }

        public string Name => _user.Name;
        public uint Id => _user.Id;

        public Server Server => _client.ServerGet(_user.Server);

        public string Identity => _client.UserGet(_user).PluginIdentity;
        public byte[] Context => _client.UserGet(_user).PluginContext.ToByteArray();

        public User GetMumbleUser()
        {
            return _user;
        }

        public Server GetMumbleServer()
        {
            return _user.Server;
        }

        public void Kick(string reason = "You have been kicked by an operator.")
        {
            _client.UserKick(new User.Types.Kick
            {
                Reason = reason,
                User = _user,
                Server = _user.Server
            }, Metadata.Empty);
        }

        public void Ban(string reason = "You have been banned by an operator.")
        {
            var s = _client.BansGet(new Ban.Types.Query
            {
                Server = _user.Server
            }).Bans.ToList();

            var dbuser = _client.DatabaseUserGet(new DatabaseUser
            {
                Id = _user.Id
            });


            s.Add(new Ban
            {
                Name = _user.Name,
                Reason = reason,
                Server = _user.Server,
                Address = _user.Address,
                Hash = dbuser.Hash,
                Start = 0,
                Bits = 32
            });

            var list = new Ban.Types.List();
            list.Server = _user.Server;
            foreach (var ban in s) list.Bans.Add(ban);

            _client.BansSet(list);
            Kick(reason);
        }

        public void Ban(TimeSpan time, string reason = "You have been banned by an operator.")
        {
            var s = _client.BansGet(new Ban.Types.Query
            {
                Server = _user.Server
            }).Bans.ToList();

            var dbuser = _client.DatabaseUserGet(new DatabaseUser
            {
                Id = _user.Id,
                Server = _user.Server
            });


            s.Add(new Ban
            {
                Name = _user.Name,
                Reason = reason,
                Server = _user.Server,
                DurationSecs = DateTimeOffset.Now.Add(time).ToUnixTimeSeconds(),
                Address = _user.Address,
                Hash = dbuser.Hash,
                Start = 0,
                Bits = 32
            });

            var list = new Ban.Types.List();
            list.Server = _user.Server;
            foreach (var ban in s) list.Bans.Add(ban);

            _client.BansSet(list);
            Kick(reason);
        }

        public void SendMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            _client.TextMessageSend(new TextMessage
            {
                Server = Server,
                Users =
                {
                    _user
                },
                Text = msg
            });
        }

        public bool HasAdmin()
        {
            var perm = _client.ACLGetEffectivePermissions(new ACL.Types.Query
            {
                User = _user,
                Channel = _client.ChannelGet(new Channel
                {
                    Id = 0,
                    Server = Server
                }),
                Server = Server
            });


            // Assuming if the user has ban on ALLOW in Root-Channel (Channel 0), then he would be an "admin".
            var hasBanSetInRoot = IsBitSet((byte) perm.Allow, (int) ACL.Types.Permission.Ban);

            return hasBanSetInRoot;
        }

        private bool IsBitSet(byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }

        public void MoveToChannel(uint channelId)
        {
            Channel channel;
            try
            {
                channel = _client.ChannelGet(new Channel
                {
                    Server = Server,
                    Id = channelId
                });
            }
            catch
            {
                return;
            }

            if (channel == null) return;

            _user.Channel = channel;
            _client.UserUpdate(_user);
        }

        // /// <summary>
        // /// AddContextAction adds a context action to the users client server region.<br/>
        // /// Added context actions are valid until:<br/>
        // /// - The context action is removed with ContextActionRemove, or<br/>
        // /// - The user disconnects from the server, or<br/>
        // /// - The server stops.<br/>
        // /// </summary>
        // /// <param name="action">The action which gets executed. (The string reference to this event)</param>
        // /// <param name="text">The text which is displayed to the user.</param>
        // public void AddContextActionServer(string action, string text)
        // {
        //
        //     _client.ContextActionAdd(new MurmurRPC.ContextAction
        //     {
        //         Action = action,
        //         Text = text,
        //         Server = Server,
        //         Context = (uint) ContextAction.Types.Context.Server,
        //         User = _user
        //     });
        // }
        //
        // /// <summary>
        // /// AddContextAction adds a context action to the users client channel menu.<br/>
        // /// Added context actions are valid until:<br/>
        // /// - The context action is removed with ContextActionRemove, or<br/>
        // /// - The user disconnects from the server, or<br/>
        // /// - The server stops.<br/>
        // /// </summary>
        // /// <param name="action">The action which gets executed. (The string reference to this event)</param>
        // /// <param name="text">The text which is displayed to the user.</param>
        // /// <param name="channel">The MumbleChannel on which the action should be displayed.</param>
        // public void AddContextActionChannel(string action, string text, MumbleChannel channel)
        // {
        //
        //     _client.ContextActionAdd(new MurmurRPC.ContextAction
        //     {
        //         Action = action,
        //         Text = text,
        //         Server = Server,
        //         Context = (uint) MurmurRPC.ContextAction.Types.Context.Server,
        //         Channel = _client.ChannelGet(new Channel{Id = channel.Id}),
        //         User = _user
        //     });
        // }
        //
        // /// <summary>
        // /// AddContextAction adds a context action to the users client users region.<br/>
        // /// Added context actions are valid until:<br/>
        // /// - The context action is removed with ContextActionRemove, or<br/>
        // /// - The user disconnects from the server, or<br/>
        // /// - The server stops.<br/>
        // /// </summary>
        // /// <param name="action">The action which gets executed. (The string reference to this event)</param>
        // /// <param name="text">The text which is displayed to the user.</param>
        // public void AddContextActionUser(string action, string text)
        // {
        //
        //     _client.ContextActionAdd(new ContextAction
        //     {
        //         Action = action,
        //         Text = text,
        //         Context = (uint) ContextAction.Types.Context.Server,
        //         User = _user
        //     });
        // }
    }
}