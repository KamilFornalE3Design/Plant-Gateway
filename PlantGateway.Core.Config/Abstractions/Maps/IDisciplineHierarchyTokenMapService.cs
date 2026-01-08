using SMSgroup.Aveva.Config.Models.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Core.Config.Abstractions.Maps
{
    public interface IDisciplineHierarchyTokenMapService
    {
        void Reload();
        DisciplineHierarchyTokenMapDTO GetMap();
        List<string> GetHierarchyForDiscipline(string discipline);
        List<string> GetTokens(string discipline, string tokenGroupKey);
    }
}
