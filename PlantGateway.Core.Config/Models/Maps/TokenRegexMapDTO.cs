using System;
using System.Collections.Generic;

namespace PlantGateway.Core.Config.Models.Maps
{
    public sealed class TokenRegexMapDTO
    {
        public Dictionary<string, TokenRegexDTO> TokenRegex { get; set; } = new Dictionary<string, TokenRegexDTO>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class TokenRegexDTO
    {
        /// <summary>
        /// The JSON key name for this token (e.g., "Plant", "Equipment", "TagIncremental").
        /// Automatically set by the map service after deserialization.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        public string Pattern { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Position { get; set; } = -1;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Marks whether this token can replace a base token (Type != "base").
        /// Automatically derived at runtime if not explicitly provided.
        /// </summary>
        public bool IsDynamic => !Type.Equals("base", StringComparison.OrdinalIgnoreCase);
    }
}
