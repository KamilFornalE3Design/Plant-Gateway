using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Application.Abstractions.Configuration.Providers
{
    public interface IEnvironmentVariablesProvider
    {
        // Connectors
        string GetPgedgeDesktopConnectorEvarName();
        string GetPgedgeDesktopConnectorEvarValue();
    }
}
