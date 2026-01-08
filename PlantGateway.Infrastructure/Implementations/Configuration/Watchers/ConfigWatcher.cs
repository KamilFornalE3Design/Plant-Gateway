using PlantGateway.Application.Abstractions.Configuration.Providers;
using PlantGateway.Application.Abstractions.Configuration.Watchers;
using PlantGateway.Application.Events.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PlantGateway.Infrastructure.Implementations.Configuration.Watchers
{
    /// <summary>
    /// Watches configuration files, folders, and environment variables for changes.
    /// Used to trigger reloads when JSON config or environment variables are updated.
    /// </summary>
    public sealed class ConfigWatcher : IConfigWatcher
    {
        private readonly IConfigProvider _configProvider;
        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly Timer _envVarTimer;
        private readonly Dictionary<string, string?> _envSnapshot = new();

        private readonly Dictionary<string, DateTime> _lastTrigger = new();
        private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(10); // adjustable

        public event EventHandler<ConfigChangedEventArgs>? ConfigChanged;
        public event EventHandler<ConfigWatcherErrorEventArgs>? ConfigError;
        public event Action? OnStopped;
        private event EventHandler? Stopped;

        private bool _isRunning;
        public bool IsRunning => _isRunning;

        public ConfigWatcher(IConfigProvider configProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            // Timer is started in WatchEvars()
            _envVarTimer = new Timer(CheckEnvVars, null, Timeout.Infinite, Timeout.Infinite);
        }

        #region Lifecycle

        /// <summary>
        /// Starts watching files, folders, and environment variables.
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;

            WatchFiles();
            WatchFolders();
            WatchEvars();

            _isRunning = true;
        }

        public void Stop()
        {
            if (!_isRunning) return;

            foreach (var watcher in _watchers)
                watcher.Dispose();

            _watchers.Clear();
            _envVarTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _isRunning = false;

            Stopped?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose() => Stop();

        #endregion

        #region File Watching

        /// <summary>
        /// Watches specific configuration files listed in appsettings.
        /// Use case: detect changes in known JSON config files.
        /// </summary>
        private void WatchFiles()
        {
            foreach (var path in _configProvider.GetAllConfigPaths())
            {
                if (!File.Exists(path))
                    continue;

                var dir = Path.GetDirectoryName(path)!;
                var file = Path.GetFileName(path);

                var watcher = new FileSystemWatcher(dir, file)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };

                watcher.Changed += (s, e) => OnFileChanged(e.FullPath);

                watcher.EnableRaisingEvents = true;
                _watchers.Add(watcher);
            }
        }

        private void OnFileChanged(string path)
        {
            try
            {
                // Debounce: ignore too-frequent triggers for same file
                if (_lastTrigger.TryGetValue(path, out var last) &&
                    DateTime.Now - last < _minInterval)
                    return;

                _lastTrigger[path] = DateTime.Now;

                ConfigChanged?.Invoke(this,
                    new ConfigChangedEventArgs("File", path, isCritical: false));
            }
            catch (Exception ex)
            {
                ConfigError?.Invoke(this, new ConfigWatcherErrorEventArgs(
                    operation: $"File",
                    path: path,
                    exception: ex));
            }
        }

        #endregion

        #region Folder Watching

        /// <summary>
        /// Watches configuration folders for new, deleted, or modified JSON files.
        /// Use case: detect addition/removal of user-specific or local settings.
        /// </summary>
        private void WatchFolders()
        {
            foreach (var path in _configProvider.GetAllConfigPaths())
            {
                if (!Directory.Exists(path))
                    continue;

                var watcher = new FileSystemWatcher(path)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    Filter = "*.json"
                };

                watcher.Created += (s, e) => OnFolderChanged("Created", e.FullPath);
                watcher.Deleted += (s, e) => OnFolderChanged("Deleted", e.FullPath);
                watcher.Changed += (s, e) => OnFolderChanged("Changed", e.FullPath);
                watcher.Renamed += (s, e) => OnFolderChanged("Renamed", e.FullPath);

                watcher.EnableRaisingEvents = true;
                _watchers.Add(watcher);
            }
        }

        private void OnFolderChanged(string action, string path)
        {
            try
            {
                // Same throttling for folders
                if (_lastTrigger.TryGetValue(path, out var last) &&
                    DateTime.Now - last < _minInterval)
                    return;

                _lastTrigger[path] = DateTime.Now;

                ConfigChanged?.Invoke(this,
                    new ConfigChangedEventArgs($"Folder-{action}", path, isCritical: false));
            }
            catch (Exception ex)
            {
                ConfigError?.Invoke(this, new ConfigWatcherErrorEventArgs(
                    operation: $"Folder",
                    path: path,
                    exception: ex));
            }
        }

        #endregion

        #region Environment Variables

        /// <summary>
        /// Begins periodic checking of configured environment variables.
        /// Use case: detect when PGEDGE_ENV or other declared variables change.
        /// </summary>
        private void WatchEvars()
        {
            _envVarTimer.Change(5000, 5000); // every 5 seconds
        }

        private void CheckEnvVars(object? state)
        {
            var envVars = _configProvider.GetEnvironmentVariables();

            foreach (var kvp in envVars)
            {
                var name = kvp.Key;
                var current = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);

                if (!_envSnapshot.TryGetValue(name, out var old))
                {
                    _envSnapshot[name] = current;
                    continue;
                }

                if (!string.Equals(current, old, StringComparison.OrdinalIgnoreCase))
                {
                    _envSnapshot[name] = current;
                    ConfigChanged?.Invoke(this,
                        new ConfigChangedEventArgs("EnvVar", name, isCritical: true));
                }
            }
        }

        #endregion
    }
}
