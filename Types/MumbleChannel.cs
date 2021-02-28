using System;
using MurmurRPC;

namespace MumbleBot.Types
{
    public class MumbleChannel
    {
        private readonly Channel _channel;
        private readonly V1.V1Client _client;

        public MumbleChannel(Channel channel, V1.V1Client client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client), "Client is null!");
            if (channel == null) throw new ArgumentNullException(nameof(channel), "Channel is null!");
            _channel = _client.ChannelGet(channel);
        }

        public string Name => _client.ChannelGet(_channel).Name;
        public string Description => _client.ChannelGet(_channel).Description;
        public uint Id => _channel.Id;

        public void SetParent(uint parentId)
        {
            var newparent = _client.ChannelGet(new Channel {Id = parentId, Server = _channel.Server});
            if (newparent == null) return;

            _channel.Parent = newparent;
            _client.ChannelUpdate(_channel);
        }

        public Channel GetMumbleChannel()
        {
            return _channel;
        }
    }
}