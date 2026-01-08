using SMSgroup.Aveva.Config.Models.DTO;

namespace PlantGateway.Core.Config.Abstractions.Maps
{
    public interface IEntityMapService
    {
        void Reload();
        EntityMapDTO GetMap();
    }
}
