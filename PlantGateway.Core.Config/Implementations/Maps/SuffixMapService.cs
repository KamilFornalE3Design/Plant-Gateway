using Newtonsoft.Json;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlantGateway.Core.Config.Implementations.Maps
{
    public class SuffixMapService : IMapService<SuffixMapDTO>, ISuffixMapService
    {
        private readonly Dictionary<MapKeys, SuffixMapDTO> _maps;
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        // Implement IMapService
        public MapKeys Key => MapKeys.Suffix;
        public string Description => "Defines tag suffix rules by Aveva hierarchy level (e.g., SITE, ZONE, EQUI).";

        public SuffixMapService(IConfigProvider configProvider, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            _maps = new Dictionary<MapKeys, SuffixMapDTO>();
            Reload();
        }
        private void LoadMap(MapKeys mapKey, string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("❌ Tag map file not found", path);

            var json = File.ReadAllText(path).Trim();
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException($"❌ Tag map file is empty: {path}");

            Dictionary<string, List<TagSuffixSchemaDTO>> parsed = null;

            try
            {
                // Try the new list-based format first
                parsed = JsonConvert.DeserializeObject<Dictionary<string, List<TagSuffixSchemaDTO>>>(json);
            }
            catch (JsonSerializationException)
            {
                // Fallback: legacy single-object format (convert to list)
                var legacy = JsonConvert.DeserializeObject<Dictionary<string, TagSuffixSchemaDTO>>(json);
                if (legacy != null)
                {
                    parsed = legacy.ToDictionary(
                        kv => kv.Key,
                        kv => new List<TagSuffixSchemaDTO>
                        {
                    kv.Value ?? new TagSuffixSchemaDTO()
                        },
                        StringComparer.OrdinalIgnoreCase);
                }
            }

            if (parsed == null || parsed.Count == 0)
                throw new InvalidOperationException("❌ Tag map JSON invalid or empty: " + path);

            // === Normalize and clean each entry ===
            foreach (var kv in parsed.ToList())
            {
                var key = kv.Key?.Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    parsed.Remove(kv.Key);
                    continue;
                }

                var schemas = kv.Value ?? new List<TagSuffixSchemaDTO>();

                foreach (var schema in schemas)
                {
                    schema.Description = schema.Description?.Trim() ?? string.Empty;
                    schema.Tokens = schema.Tokens?
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Select(t => t.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()
                        ?? new List<string>();
                }

                parsed[key] = schemas;
            }

            // === Store result ===
            _maps[mapKey] = new SuffixMapDTO
            {
                Tags = new Dictionary<string, List<TagSuffixSchemaDTO>>(parsed, StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// Returns a distinct list of all tokens defined in TagMap for a given node type (role).
        /// Example: "SITE" -> ["Discipline", "Entity"], "EQUI" -> ["TagComposite", "Component", "Electrical"]
        /// </summary>
        public List<string> GetTokensForRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return new List<string>();

            if (!_maps.TryGetValue(Key, out var map) || map?.Tags == null)
                return new List<string>();

            if (!map.Tags.TryGetValue(role, out var schemas) || schemas == null || schemas.Count == 0)
                return new List<string>();

            // Merge all token lists for this role and remove duplicates
            return schemas
                .Where(s => s?.Tokens != null)
                .SelectMany(s => s.Tokens)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Strongly typed (for engines)
        public SuffixMapDTO GetMap() => _maps[Key];

        // Untyped (for CLI/UI manager)
        public object GetMapUntyped() => GetMap();

        public string GetFilePath() => _configProvider.GetSuffixMapPath();

        public void Reload() => LoadMap(Key, GetFilePath());
    }
}
