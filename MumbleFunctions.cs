using System.Collections.Generic;
using System.Linq;
using Grpc.Core;
using MumbleBot.ContextAction;
using MumbleBot.Types;
using MurmurRPC;

namespace MumbleBot
{
    public partial class Mumble
    {
        private readonly Dictionary<string, MumbleContextAction> registeredContextActions = new();

        /// <summary>
        ///     Get an exact user with their id.
        /// </summary>
        /// <param name="id">The id to look for.</param>
        /// <returns>A <see cref="User">User</see> object or null.</returns>
        public MumbleUser GetUser(uint id)
        {
            return GetAllUsers().Find(user => user.Id == id);
        }

        /// <summary>
        ///     Get an exact user with their name.
        /// </summary>
        /// <param name="name">The name to look for.</param>
        /// <returns>A <see cref="MumbleUser">User</see> object or null.</returns>
        public MumbleUser GetUser(string name)
        {
            return GetAllUsers().Find(user => user.Name == name);
        }

        /// <summary>
        ///     Get a list of users with similarity to "name".
        /// </summary>
        /// <param name="name">The name to look for.</param>
        /// <param name="edits">
        ///     The maximum edits for a name to be included. See
        ///     <a href="https://en.wikipedia.org/wiki/Levenshtein_distance">Levenshtein distance</a>
        /// </param>
        /// <returns>A <see cref="MumbleUser">User</see> object or null.</returns>
        public List<MumbleUser> GetSimilarUser(string name, double edits = 5.0)
        {
            return GetAllUsers().FindAll(user => LevenshteinDistance.Compute(user.Name, name) < edits);
        }


        /// <summary>
        ///     Gets all users.
        /// </summary>
        /// <returns>Returns a <see cref="List{T}">List</see> of users.</returns>
        public List<MumbleUser> GetAllUsers()
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

        /// <summary>
        ///     Kick a user by their id.
        /// </summary>
        /// <param name="id">The user's id.</param>
        /// <param name="reason">The reason to kick them.</param>
        public void KickUser(uint id, string reason)
        {
            var user = GetUser(id);
            _client.UserKick(new User.Types.Kick
            {
                User = user.GetMumbleUser(),
                Reason = reason,
                Server = user.GetMumbleServer()
            });
        }

        /// <summary>
        ///     Kick a user by name.
        /// </summary>
        /// <param name="name">The user's name</param>
        /// <param name="reason">The reason to kick them.</param>
        public void KickUser(string name, string reason)
        {
            var user = GetUser(name);
            if (user == null) return;
            KickUser(user.Id, reason);
        }

        /// <summary>
        ///     Get a specific Server by id.
        /// </summary>
        /// <param name="id">The server id.</param>
        /// <returns>Returns the server specified with <paramref name="id">id</paramref>.</returns>
        public MumbleServer GetServer(uint id)
        {
            return GetAllServers().Find(server => server.Id == id);
        }

        /// <summary>
        ///     Get every server.
        /// </summary>
        /// <returns>Returns a <see cref="List{T}">list</see> of servers</returns>
        public List<MumbleServer> GetAllServers()
        {
            var servers = new List<MumbleServer>();
            foreach (var server in _client.ServerQuery(new Server.Types.Query(), Metadata.Empty).Servers.ToList())
                servers.Add(new MumbleServer(server, _client));

            return servers;
        }

        /// <summary>
        ///     Gets the complete config of the server.
        /// </summary>
        /// <param name="id">The server's id.</param>
        /// <returns>The server's config.</returns>
        public Config GetConfig(uint id)
        {
            return _client.ConfigGet(new Server
            {
                Id = id
            });
        }

        /// <summary>
        ///     Gets the server's uptime.
        /// </summary>
        /// <param name="id">The server's id.</param>
        /// <returns>The time in seconds, the server has been up.</returns>
        public ulong GetUptime(uint id)
        {
            return _client.GetUptime(new Void()).Secs;
        }

        /// <summary>
        ///     Gets the server's version.
        /// </summary>
        /// <returns>The server's version.</returns>
        public Version GetVersion()
        {
            return _client.GetVersion(new Void());
        }

        /// <summary>
        ///     Creates a new Serverinstance.
        /// </summary>
        /// <returns>Returns a <see cref="MumbleServer" /> instance.</returns>
        public MumbleServer CreateServer()
        {
            return new(_client.ServerCreate(new Void()), _client);
        }

        /// <summary>
        ///     Starts the given server.
        /// </summary>
        /// <param name="id">The id of the server.</param>
        public void StartServer(uint id)
        {
            if (GetServer(id) == null)
            {
                logger.Warn($"Tried to start server {id} but the server does not exist.");
                return;
            }

            if (GetServer(id).Running)
            {
                logger.Warn($"Tried to start server {id} but the server is already running.");
                return;
            }

            _client.ServerStart(new Server
            {
                Id = id
            });
        }

        /// <summary>
        ///     Stops the given server.
        /// </summary>
        /// <param name="id">The server's id.</param>
        public void StopServer(uint id)
        {
            if (GetServer(id) == null)
            {
                logger.Warn($"Tried to stop server {id} but the server does not exist.");
                return;
            }

            if (!GetServer(id).Running)
            {
                logger.Warn($"Tried to stop server {id} but the server is not running.");
                return;
            }

            _client.ServerStop(new Server
            {
                Id = id
            });
        }

        /// <summary>
        ///     Deletes the given server.
        /// </summary>
        /// <param name="id">The server's id.</param>
        public void DeleteServer(uint id)
        {
            if (GetServer(id) == null)
            {
                logger.Warn($"Tried to delete server {id} but the server does not exist.");
                return;
            }

            _client.ServerRemove(new Server
            {
                Id = id
            });
        }


        /// <summary>
        ///     Query the default config.
        /// </summary>
        /// <returns>Returns the default config for all servers.</returns>
        public MumbleDefaultConfig GetDefaultConfig()
        {
            return new(_client.ConfigGetDefault(new Void()), _client);
        }

        public List<MumbleDBUser> GetAllDBUsers()
        {
            var ret = new List<MumbleDBUser>();
            foreach (var server in GetAllServers())
            {
                var dbUsers = _client.DatabaseUserQuery(new DatabaseUser.Types.Query
                {
                    Server = server.GetMumbleServer()
                });

                foreach (var dbUsersUser in dbUsers.Users) ret.Add(new MumbleDBUser(dbUsersUser, _client));
            }

            return ret;
        }

        public MumbleContextAction CreateContextAction(string action, string text, MumbleServer server,
            ContextActionContext context)
        {
            return CreateContextAction(action, text, server, null, null, context);
        }

        public MumbleContextAction CreateContextAction(string action, string text, MumbleServer server, MumbleUser user,
            ContextActionContext context)
        {
            return CreateContextAction(action, text, server, null, user, context);
        }

        public MumbleContextAction CreateContextAction(string action, string text, MumbleServer server,
            MumbleChannel channel, ContextActionContext context)
        {
            return CreateContextAction(action, text, server, channel, null, context);
        }

        public MumbleContextAction CreateContextAction(string action, string text, MumbleServer server,
            MumbleChannel channel, MumbleUser user, ContextActionContext context)
        {
            if (registeredContextActions.ContainsKey(action)) return null;

            var listener = new MumbleContextAction(action, text, server, channel, user, context, _client);

            registeredContextActions.Add(action, listener);

            return listener;
        }
    }
}