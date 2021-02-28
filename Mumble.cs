using System.Linq;
using Grpc.Net.Client;
using log4net;
using MurmurRPC;

namespace MumbleBot
{
    public partial class Mumble
    {
        internal V1.V1Client _client;

        private readonly ILog logger;

        public Mumble(string address = "http://127.0.0.1:50051")
        {
            Address = address;
            logger = LogManager.GetLogger("Mumble");
            eventLogger = LogManager.GetLogger("Events");

            var channel = GrpcChannel.ForAddress(address);

            _client = new V1.V1Client(channel);
        }

        public static Mumble Instance { get; internal set; }

        private string Address { get; }

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

                    switch (cmd)
                    {
                        case "stop":
                        {
                            if (user.HasAdmin())
                            {
                                user.SendMessage("Ok, Requesting shutdown... Thank you!");
                                Program.RequestStop();
                            }

                            break;
                        }
                        case "ping":
                            user.SendMessage("Pong");
                            break;
                    }
                }
            };

            UserConnected += delegate(object? sender, UserConnectedEvent ev)
            {
                eventLogger.Info($"User {ev.User.Name} ({ev.User.Id}) joined.");
                var user = ev.User;

                foreach (var contextAction in registeredContextActions)
                    _client.ContextActionAdd(new MurmurRPC.ContextAction
                    {
                        Action = contextAction.Value.Action,
                        Channel = contextAction.Value.Channel?.GetMumbleChannel(),
                        Server = contextAction.Value.Server?.GetMumbleServer(),
                        Context = (uint) contextAction.Value.Context,
                        Text = contextAction.Value.Text,
                        User = user.GetMumbleUser()
                    });
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