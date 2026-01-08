using SMSgroup.Aveva.Config.Models.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Core.Config.Abstractions.Maps
{
    public interface ICodificationMapService
    {
        void Reload();
        CodificationMapDTO GetMap();

        bool IsKnownPlant(string inputCode);
        bool IsKnownPlantUnit(string inputCode);
        bool IsKnownPlantSection(string inputCode);
        bool IsKnownEquipment(string inputCode);

        List<string> GetKnownPlants();
        List<string> GetKnownPlantUnits(string plantCode);
        List<string> GetKnownPlantSections(string plantUnitCode);
        List<string> GetKnownEquipment(string plantSectionCode);

        CodificationType GetCodificationType(string inputCode);
    }
}
