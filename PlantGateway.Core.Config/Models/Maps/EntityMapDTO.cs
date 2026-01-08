using System;
using System.Collections.Generic;

namespace PlantGateway.Core.Config.Models.Maps
{
    /// <summary>
    /// Defines all supported AVEVA entity types (e.g., SDE, NOZ, DATUM).
    /// Provides metadata for classification, import automation, and validation.
    /// </summary>
    public class EntityMapDTO
    {
        public Dictionary<string, EntityDTO> Entities { get; set; } = new Dictionary<string, EntityDTO>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Represents a single entity type entry. => it is a temp holder so the fields are not logical.
    /// </summary>
    public class EntityDTO
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty; 
    }
}
