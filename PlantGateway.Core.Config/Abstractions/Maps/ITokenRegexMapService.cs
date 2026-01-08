using SMSgroup.Aveva.Config.Models.DTO;
using System.Collections.Generic;

namespace PlantGateway.Core.Config.Abstractions.Maps
{
    public interface ITokenRegexMapService
    {
        void Reload();
        TokenRegexMapDTO GetMap();
        TokenRegexDTO GetTokenForPosition(int position);
        IReadOnlyList<string> GetDefaultNameOrder();
        int GetHighestBasePosition();
    }
}
