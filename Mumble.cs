using System.Linq;
using Grpc.Net.Client;
using log4net;
using MurmurRPC;

namespace MumbleBot
{
    public partial class Mumble
    {
        internal V1.V1Client _client;

        private ILog logger;

        public static Mumble Instance { get; internal set; }

        private string Address { get; set; }

        public Mumble(string address = "http://127.0.0.1:50051")
        {
            this.Address = address;
            logger = LogManager.GetLogger("Mumble");
            eventLogger = LogManager.GetLogger("Events");
            var channel = GrpcChannel.ForAddress(address);
            _client = new V1.V1Client(channel);
        }

        internal void Start()
        {
            // var mumbleServerEvents = new Thread(MumbleServerEventThread) {Name = "MumbleServerEventThread"};
            // mumbleServerEvents.Start();

            RunServerEventThread();
            RunVServerEventThreads();

            UserTextMessage += delegate(object? sender, UserTextMessageEvent e)
            {
                var msg = e.Message.Text;
                var ch = e.Channels;
                var user = e.User;
                var server = e.Server;

                if (msg == "!bot")
                {
                    user.SendMessage("<br><br>You need to specify some arguments! <b>!bot</b> <i>cmd</i> args...");
                    return;
                }

                if (msg.StartsWith("!bot"))
                {
                    var cmd = msg.Split(" ")[1];
                    var args = msg.Split(" ").TakeLast(msg.Split(" ").Length - 2).ToArray();

                    if (cmd == "stop")
                    {
                        if (user.HasAdmin())
                        {
                            user.SendMessage("Ok, Requesting shutdown... Thank you!");
                            Program.RequestStop();
                        }
                    }
                }
            };

            // var vServerEvents = new Thread(MumbleVServerEventThread) {Name = "MumbleVServerEventThread"};
            // vServerEvents.Start();
        }

        public void Shutdown()
        {
            Program.RequestStop();
        }
    }
}