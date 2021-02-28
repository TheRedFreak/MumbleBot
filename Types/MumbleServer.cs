using System;
using System.Collections.Generic;
using System.Linq;
using Grpc.Core;
using log4net;
using MurmurRPC;

namespace MumbleBot.Types
{
    public class MumbleServer
    {
        private ILog logger = Program.logger;

        public MumbleServer(Server server, V1.V1Client mumble)
        {
            Server = server;
            _client = mumble;
        }

        private Server Server { get; }
        private V1.V1Client _client { get; }
        public uint Id => Server.Id;

        public long Uptime
        {
            get
            {
                var server = _client.ServerGet(Server);
                if (server != null) return Convert.ToInt32(Server.Uptime.Secs);
                return -1;
            }
        }

        public bool Running => Server.Running;

        public MumbleChannel Root => GetChannel(0);

        public string Name => GetConfig().GetValue("registerName");

        public Server GetMumbleServer()
        {
            return Server;
        }

        public MumbleUser GetUserByName(string name)
        {
            return GetUsers().Find(user => user.Name == name);
        }

        public MumbleUser GetUserById(uint id)
        {
            return GetUsers().Find(user => user.Id == id);
        }

        public List<MumbleUser> GetUsers()
        {
            var users = new List<MumbleUser>();
            var servers = _client.ServerQuery(new Server.Types.Query(), Metadata.Empty);
            foreach (var server in servers.Servers)
            {
                var lusers = _client.UserQuery(new User.Types.Query
                {
                    Server = server
                });

                foreach (var luser in lusers.Users) users.Add(new MumbleUser(luser, _client));
            }

            return users;
        }

        public List<MumbleBan> GetBans()
        {
            var tmp = _client.BansGet(new Ban.Types.Query
            {
                Server = Server
            }).Bans.ToList();

            var bans = new List<MumbleBan>();

            foreach (var ban in tmp) bans.Add(new MumbleBan(ban));

            return bans;
        }

        public void UnbanUser(string name)
        {
            var oldbans = _client.BansGet(new Ban.Types.Query
            {
                Server = Server
            }).Bans.ToList();

            var list = new Ban.Types.List();
            list.Server = Server;
            foreach (var ban in oldbans)
                if (ban.Name != name)
                    list.Bans.Add(ban);

            _client.BansSet(list);
        }

        public MumbleConfig GetConfig()
        {
            return new(_client.ConfigGet(Server), _client);
        }

        public void KickUser(uint id, string reason)
        {
            var user = _client.UserGet(new User
            {
                Id = id
            });

            if (user.Server.Id == Server.Id)
                _client.UserKick(new User.Types.Kick
                {
                    Reason = reason,
                    Server = Server,
                    User = user
                });
        }

        public void KickUser(string name, string reason)
        {
            var user = GetUserByName(name);
            KickUser(user.Id, reason);
        }

        public void BanUser(uint id, string reason)
        {
            var user = _client.UserGet(new User
            {
                Id = id
            });

            if (user.Server.Id == Server.Id)
                _client.UserKick(new User.Types.Kick
                {
                    Reason = reason,
                    Server = Server,
                    User = user
                });
        }


        public MumbleTreeChannel GetChannelTree()
        {
            var tree = _client.TreeQuery(new Tree.Types.Query
            {
                Server = Server
            });


            var r = RecurseThing(tree);

            return r;
        }

        private MumbleTreeChannel RecurseThing(Tree tree)
        {
            var root = new MumbleTreeChannel
            {
                Channel = new MumbleChannel(tree.Channel, _client),
                Users = tree.Users.Select(x => new MumbleUser(x, _client)).ToList()
            };


            if (tree.Children.Count == 0) return root;

            foreach (var treeChild in tree.Children) root.Children.Add(RecurseThing(treeChild));

            return root;
        }

        public MumbleChannel GetChannel(uint id)
        {
            return new(_client.ChannelGet(new Channel
            {
                Id = id,
                Server = Server
            }), _client);
        }

        public MumbleChannel CreateChannel(string name, string description = "")
        {
            return new(_client.ChannelAdd(new Channel
            {
                Server = Server,
                Name = name,
                Description = description ?? "",
                Parent = _client.ChannelGet(new Channel
                {
                    Id = 0,
                    Server = Server
                })
            }), _client);
        }
    }
}