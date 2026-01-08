using System;
using System.Collections.Generic;

namespace PlantGateway.Core.Config.Models.Maps
{
    public sealed class RoleMapDTO
    {
        public Dictionary<string, RoleDTO> Roles { get; set; } = new Dictionary<string, RoleDTO>(StringComparer.OrdinalIgnoreCase);
    }
    public sealed class RoleDTO
    {
        public string AvevaType { get; set; } = string.Empty;
        public string BusinessConcept { get; set; } = string.Empty;
        public bool IsLeaf { get; set; } = false;
        public List<string> DisciplineGroups { get; set; } = new List<string>();
        public string Description { get; set; } = string.Empty;
    }
}
