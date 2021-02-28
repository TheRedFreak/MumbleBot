using System.Collections.Generic;

namespace MumbleBot.Types
{
    public class MumbleTreeChannel
    {
        public MumbleChannel Channel { get; set; }
        public List<MumbleTreeChannel> Children { get; set; } = new();
        public List<MumbleUser> Users { get; set; } = new();
    }
}