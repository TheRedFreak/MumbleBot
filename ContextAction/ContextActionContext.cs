using System;

namespace MumbleBot.ContextAction
{
    [Flags]
    public enum ContextActionContext
    {
        Server = 0x01,
        Channel = 0x02,
        User = 0x04
    }
}