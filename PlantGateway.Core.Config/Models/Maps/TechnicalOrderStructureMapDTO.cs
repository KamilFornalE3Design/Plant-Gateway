using System;
using System.Collections.Generic;

namespace PlantGateway.Core.Config.Models.Maps
{
    public sealed class TechnicalOrderStructureMapDTO
    {
        public Dictionary<string, TechnicalOrderStructureSchemaDTO> Structures { get; set; } = new Dictionary<string, TechnicalOrderStructureSchemaDTO>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class TechnicalOrderStructureSchemaDTO
    {
        public List<string> Tokens { get; set; } = new List<string>();
        public string Description { get; set; } = string.Empty;
    }
}
