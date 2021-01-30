using System.Net;
using MurmurRPC;

namespace MumbleBot.Types
{
    public class MumbleBan
    {
        private readonly MurmurRPC.Ban _ban;

        public Server Server => _ban.Server;
        public IPAddress Address => new IPAddress(_ban.Address.ToByteArray());
        public string Hash => _ban.Hash;
        public string Reason => _ban.Reason;
        public long Start => _ban.Start;
        public long DurationSecs => _ban.DurationSecs;
        public string Name => _ban.Name;

        public MumbleBan(MurmurRPC.Ban ban)
        {
            _ban = ban;
        }
    }
}