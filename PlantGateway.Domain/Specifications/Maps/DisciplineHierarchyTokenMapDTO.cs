using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace PlantGateway.Domain.Specifications.Maps
{
    // DisciplineHierarchyTokenMapDTO (map)
    // └── "ST" : DisciplineTokenDefinitionDTO
    //     ├── Hierarchy : List<string>
    //     └── Tokens : Dictionary<string, TokenGroupDTO>
    //         └── "ZONE"
    //             ├── Affix  : List<string>
    //             ├── Base   : List<string>
    //             └── Suffix : List<string>

    //var map = _disciplineHierarchyTokenMapService.GetMap().Disciplines;
    //var disciplineDef = map.TryGetValue(discipline, out var def) ? def : map["DEFAULT"];
    //var schema = disciplineDef.Tokens.TryGetValue(role, out var s) ? s : map["DEFAULT"].Tokens[role];

    public class DisciplineHierarchyTokenMapDTO
    {
        public Dictionary<string, DisciplineDefinitionDTO> Disciplines { get; set; } = new Dictionary<string, DisciplineDefinitionDTO>(StringComparer.OrdinalIgnoreCase);
    }

    public class DisciplineDefinitionDTO
    {
        public List<string> Hierarchy { get; set; } = new List<string>();
        public Dictionary<string, TokenGroupDTO> Tokens { get; set; } = new Dictionary<string, TokenGroupDTO>(StringComparer.OrdinalIgnoreCase);
    }

    public class TokenGroupDTO
    {
        public List<string> Affix { get; set; } = new List<string>();
        public List<string> Base { get; set; } = new List<string>();
        public List<string> Suffix { get; set; } = new List<string>();
    }
}
