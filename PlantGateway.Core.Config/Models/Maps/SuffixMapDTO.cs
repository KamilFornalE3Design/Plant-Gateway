using System;
using System.Collections.Generic;

namespace PlantGateway.Core.Config.Models.Maps
{
    public sealed class SuffixMapDTO
    {
        public Dictionary<string, List<TagSuffixSchemaDTO>> Tags { get; set; } = new Dictionary<string, List<TagSuffixSchemaDTO>>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class TagSuffixSchemaDTO
    {
        public List<string> Tokens { get; set; } = new List<string>();
        public string Description { get; set; } = string.Empty;
    }
}
