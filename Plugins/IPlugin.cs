using log4net;

namespace MumbleBot
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        string Version { get; }

        ILog logger { set; }
        Mumble _mumble { set; }

        void Load();

        void Start();

        void Stop();
    }
}