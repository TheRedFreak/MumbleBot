using MurmurRPC;

namespace MumbleBot
{
    public class MumbleDBUser
    {
        private readonly V1.V1Client _client;
        private readonly DatabaseUser _user;

        public MumbleDBUser(DatabaseUser user, V1.V1Client client)
        {
            _client = client;
            _user = _client.DatabaseUserGet(user);
        }

        public string Name => _user.Name;
        public string Comment => _user.Comment;
        public string Email => _user.Email;
        public string Hash => _user.Hash;
        public string LastActive => _user.LastActive;
    }
}