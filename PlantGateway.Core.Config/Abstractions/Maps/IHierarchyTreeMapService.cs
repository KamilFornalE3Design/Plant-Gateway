using SMSgroup.Aveva.Config.Models.DTO;
using System.Collections.Generic;

namespace PlantGateway.Core.Config.Abstractions.Maps
{
    public interface IHierarchyTreeMapService
    {
        HierarchyTreeMapDTO GetMap();
        List<string> GetHierarchyForDiscipline(string discipline);
    }
}
