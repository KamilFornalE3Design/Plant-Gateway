using System;
using System.Collections.Generic;

namespace PlantGateway.Core.Config.Models.Maps
{
    public sealed class HierarchyTreeMapDTO
    {
        public Dictionary<string, HierarchyTreeDTO> HierarchyTrees { get; set; } = new Dictionary<string, HierarchyTreeDTO>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class HierarchyTreeDTO
    {
        public List<string> Hierarchy { get; set; } = new List<string>();
        public string Description { get; set; } = string.Empty;
    }
}
