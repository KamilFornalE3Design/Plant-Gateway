using PlantGateway.Application.Events.Configuration;

namespace PlantGateway.Application.Abstractions.Configuration.Watchers
{
    public interface IConfigWatcher : IDisposable
    {
        void Start();
        void Stop();
        event EventHandler<ConfigChangedEventArgs> ConfigChanged;
        event EventHandler<ConfigWatcherErrorEventArgs> ConfigError;

        bool IsRunning { get; }
    }
}
