using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PlantGateway.Core.Config.Models.Maps
{
    /// <summary>
    /// Data transfer object (DTO) representing a header mapping configuration
    /// loaded from a JSON file (e.g. InputHeadersMap.json).
    ///
    /// Contains:
    /// <list type="bullet">
    /// <item>
    ///   <term><see cref="Headings"/></term>
    ///   <description>
    ///     A dictionary mapping raw column names (from TXT, CSV, Excel, etc.)
    ///     to normalized header names used internally in Plant Gateway.
    ///   </description>
    /// </item>
    /// <item>
    ///   <term><see cref="Groups"/></term>
    ///   <description>
    ///     A dictionary of semantic groups of headers.
    ///     Each group defines a set of related raw columns that should be
    ///     interpreted together as one logical entity, such as POSITION or ORIENTATION.
    ///   </description>
    /// </item>
    /// </list>
    ///
    /// By externalizing this data in JSON, the system becomes flexible:
    /// changes in input file structure or naming conventions can be handled
    /// without modifying code — only the JSON configuration needs updating.
    /// </summary>
    public sealed class HeaderMapDTO
    {
        /// <summary>
        /// Maps raw input column names to normalized names.
        /// Keys are raw headers; values are their normalized equivalents.
        /// Case-insensitive lookups are supported.
        /// </summary>
        [JsonProperty("Headings")]
        public Dictionary<string, string> Headings { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Groups of related raw headers that together form a semantic unit
        /// (e.g. POSITION or ORIENTATION).
        /// Keys are group names; values are arrays of raw header strings.
        /// Case-insensitive lookups are supported.
        /// </summary>
        [JsonProperty("Groups")]
        public Dictionary<string, string[]> Groups { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
    }
}
