using PlantGateway.Application.Abstractions.Configuration.Watchers;
using PlantGateway.Application.Events.Configuration;
using PlantGateway.Presentation.CLI.Rendering.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Presentation.CLI.Rendering.Config
{
    public sealed class ConfigWatcherRenderer : IDisposable
    {
        private readonly IConfigWatcher _watcher;
        private readonly MarkupLineRenderer _line;

        public ConfigWatcherRenderer(IConfigWatcher watcher, MarkupLineRenderer line)
        {
            _watcher = watcher;
            _line = line;

            _watcher.ConfigChanged += OnConfigChanged;

            // If you adopt the Error event:
            _watcher.ConfigError += OnError;
        }

        private void OnConfigChanged(object? sender, ConfigChangedEventArgs e)
        {
            var message = $"Config changed [{e.Identifier}]: {e.Source}";

            if (e.IsCritical)
                _line.Warning(message);
            else
                _line.Muted(message);
        }

        private void OnError(object? sender, ConfigWatcherErrorEventArgs e)
        {
            _line.Error($"ConfigWatcher error ({e.Operation}): {e.Exception.Message}");
        }

        public void Dispose()
        {
            _watcher.ConfigChanged -= OnConfigChanged;
            _watcher.ConfigError -= OnError;
        }
    }
}
