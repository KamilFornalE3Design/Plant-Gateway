using System;
using System.Collections.Generic;

namespace PlantGateway.Domain.Specifications.Maps
{
    public sealed class AllowedTreeMapDTO
    {
        public Dictionary<string, AllowedTreeDTO> AllowedTree { get; set; } = new Dictionary<string, AllowedTreeDTO>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class AllowedTreeDTO
    {
        public List<string> AllowedOwners { get; set; } = new List<string>();
        public List<string> AllowedChildren { get; set; } = new List<string>();
        public string Description { get; set; } = string.Empty;
    }
}
