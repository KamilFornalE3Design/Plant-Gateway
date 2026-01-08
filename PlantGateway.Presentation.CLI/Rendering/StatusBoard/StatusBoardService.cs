using Microsoft.Extensions.DependencyInjection;
using PlantGateway.Application.Abstractions.Configuration.Watchers;
using PlantGateway.Infrastructure.Implementations.Configuration.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Presentation.CLI.Rendering.StatusBoard
{
    public sealed class StatusBoardService
    {
        private readonly IServiceProvider _provider;

        public StatusBoardService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public StatusBoardState GetCurrentState()
        {
            // Resolve values from services (no rendering here)
            var watcher = _provider.GetService<IConfigWatcher>();

            return new StatusBoardState
            {
                Environment = ResolveEnvironment(),
                PipeBridge = "BridgeServer", // TODO: resolve from your bridge/session manager when available
                ConfigWatcherOn = watcher?.IsRunning ?? false
            };
        }

        private static string ResolveEnvironment()
        {
            // Keep this host-level. If you later move env resolution into Application, call a port instead.
            return AppEnvironmentResolver.ResolveAppEnv(string.Empty).ToString();
        }
    }
}
