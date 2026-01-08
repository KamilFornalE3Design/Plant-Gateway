using SMSgroup.Aveva.Config.Models.DTO;
using System.Collections.Generic;

namespace PlantGateway.Core.Config.Abstractions.Maps
{
    public interface ISuffixMapService
    {
        SuffixMapDTO GetMap();
        List<string> GetTokensForRole(string role);
    }
}
