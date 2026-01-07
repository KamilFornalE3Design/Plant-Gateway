using PlantGateway.Application.Abstractions.Configuration.Providers;
using PlantGateway.Infrastructure.Implementations.Configuration.Accessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Infrastructure.Implementations.Configuration.Providers
{
    public class EnvironmentVariablesProvider : IEnvironmentVariablesProvider
    {
        private readonly ConfigurationAccessor _configAccessor;

        public EnvironmentVariablesProvider(ConfigurationAccessor configAccessor)
        {
            _configAccessor = configAccessor;
        }

        public string GetPgedgeDesktopConnectorEvarName() => "PGEDGE_CLI_LAUNCHER";
        public string GetPgedgeDesktopConnectorEvarValue() =>
            _configAccessor.GetRequired("EnvironmentVariables:PGEDGE_CLI_LAUNCHER");
    }
}
