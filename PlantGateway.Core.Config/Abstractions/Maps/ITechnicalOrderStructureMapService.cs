using SMSgroup.Aveva.Config.Models.DTO;
using System.Collections.Generic;

namespace PlantGateway.Core.Config.Abstractions.Maps
{
    public interface ITechnicalOrderStructureMapService
    {
        TechnicalOrderStructureMapDTO GetMap();
        List<string> GetTokens(string nodeType);
    }
}
