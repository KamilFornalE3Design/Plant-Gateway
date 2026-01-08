using PlantGateway.Core.Config.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Core.Config.Abstractions
{
    public interface IAppEnvironmentCatalog
    {
        IReadOnlyDictionary<string, string> GetEnvironmentVariables();
    }
}
