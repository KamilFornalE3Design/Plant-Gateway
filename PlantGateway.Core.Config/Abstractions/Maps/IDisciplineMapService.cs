using SMSgroup.Aveva.Config.Models.DTO;

namespace PlantGateway.Core.Config.Abstractions.Maps
{
    public interface IDisciplineMapService
    {
        void Reload();
        DisciplineMapDTO GetMap();
    }
}
