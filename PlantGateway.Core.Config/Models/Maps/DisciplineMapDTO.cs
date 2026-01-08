using System;
using System.Collections.Generic;

namespace PlantGateway.Core.Config.Models.Maps
{
    public class DisciplineMapDTO
    {
        public Dictionary<string, DisciplineDTO> Disciplines { get; set; } = new Dictionary<string, DisciplineDTO>(StringComparer.OrdinalIgnoreCase);
    }
    /// <summary>
    /// Discipline DTO loaded from JSON config.
    /// <summary>
    public class DisciplineDTO
    {
        public string Code { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public int Order { get; set; } = int.MinValue;
        public string Family { get; set; } = string.Empty;
    }
}
