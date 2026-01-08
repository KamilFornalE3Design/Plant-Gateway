using PlantGateway.Core.Config.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Application.Abstractions.Configuration.Providers
{
    public interface IConfigProvider : IConfigPathsProvider, IAppEnvironmentCatalog
    {

    }
}
