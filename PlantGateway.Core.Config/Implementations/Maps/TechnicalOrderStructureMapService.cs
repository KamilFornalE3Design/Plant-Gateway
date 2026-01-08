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
    public class TechnicalOrderStructureMapService : IMapService<TechnicalOrderStructureMapDTO>, ITechnicalOrderStructureMapService
    {
        private readonly Dictionary<MapKeys, TechnicalOrderStructureMapDTO> _maps;
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        // Implement IMapService
        public MapKeys Key => MapKeys.TOS;
        public string Description => "...";

        public TechnicalOrderStructureMapService(IConfigProvider configProvider, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            _maps = new Dictionary<MapKeys, TechnicalOrderStructureMapDTO>();

            LoadMap(Key, GetFilePath());
        }

        private void LoadMap(MapKeys mapKey, string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("❌ TechnicalOrderStructure map file not found", path);

            var json = File.ReadAllText(path).Trim();

            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException($"❌ TechnicalOrderStructure map file is empty: {path}");

            // ✅ New shape: flat dictionary (no "Structures" wrapper)
            var raw = JsonConvert.DeserializeObject<Dictionary<string, TechnicalOrderStructureSchemaDTO>>(json)
                      ?? throw new InvalidOperationException("❌ Invalid TechnicalOrderStructureMap JSON format (null dictionary).");

            if (raw.Count == 0)
                throw new InvalidOperationException("❌ TechnicalOrderStructureMap JSON contains no entries: " + path);

            // ✅ Normalize
            var dto = new TechnicalOrderStructureMapDTO
            {
                Structures = raw
                    .Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
                    .ToDictionary(
                        kv => kv.Key.Trim(),
                        kv =>
                        {
                            var s = kv.Value ?? new TechnicalOrderStructureSchemaDTO();
                            s.Description = s.Description?.Trim() ?? string.Empty;
                            s.Tokens = s.Tokens?.Where(t => !string.IsNullOrWhiteSpace(t))
                                .Select(t => t.Trim())
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList()
                                ?? new List<string>();
                            return s;
                        },
                        StringComparer.OrdinalIgnoreCase)
            };

            _maps[mapKey] = dto;
        }

        /// <summary>
        /// Returns the token list for the given node type (e.g. SITE, ZONE).
        /// Used for base-name generation in hierarchy.
        /// </summary>
        public List<string> GetTokens(string nodeType)
        {
            if (_maps[Key].Structures.TryGetValue(nodeType, out var schema))
                return schema.Tokens ?? new List<string>();

            return new List<string>();
        }

        // Strongly typed (for engines)
        public TechnicalOrderStructureMapDTO GetMap() => _maps[Key];

        // Untyped (for CLI/UI manager)
        public object GetMapUntyped() => GetMap();

        public string GetFilePath() => _configProvider.GetTechnicalOrderStructureMapPath();

        public void Reload() => LoadMap(Key, GetFilePath());
    }
}
