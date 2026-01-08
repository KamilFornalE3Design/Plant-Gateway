using SMSgroup.Aveva.Config.Models.DTO;

namespace PlantGateway.Core.Config.Abstractions.Maps
{
    public interface ICatrefMapService
    {
        void Reload();
        CatrefMapDTO GetMap();
    }
}
