using Newtonsoft.Json;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlantGateway.Core.Config.Implementations.Maps
{
    public class HierarchyTreeMapService : IMapService<HierarchyTreeMapDTO>, IHierarchyTreeMapService
    {
        private readonly Dictionary<MapKeys, HierarchyTreeMapDTO> _maps;
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        // Implement IMapService
        public MapKeys Key => MapKeys.Hierarchy;
        public string Description => "Defines AVEVA structure hierarchy and allowed disciplines.";

        public HierarchyTreeMapService(IConfigProvider configProvider, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            _maps = new Dictionary<MapKeys, HierarchyTreeMapDTO>();

            Reload();
        }

        private void LoadMap(MapKeys mapKey, string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("❌ HierarchyTree map file not found", path);

            var json = File.ReadAllText(path).Trim();
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException($"❌ HierarchyTree map file is empty: {path}");

            // ✅ Expected structure: { "HierarchyTrees": { ... } }
            var dto = JsonConvert.DeserializeObject<HierarchyTreeMapDTO>(json)
                      ?? throw new InvalidOperationException("❌ Invalid HierarchyTree map JSON format (null object).");

            if (dto.HierarchyTrees == null || dto.HierarchyTrees.Count == 0)
                throw new InvalidOperationException("❌ HierarchyTree map JSON contains no hierarchy definitions: " + path);

            // ✅ Normalize keys and clean data
            var cleaned = new Dictionary<string, HierarchyTreeDTO>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in dto.HierarchyTrees)
            {
                var key = kv.Key?.Trim();
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                var tree = kv.Value ?? new HierarchyTreeDTO();
                tree.Description = tree.Description?.Trim() ?? string.Empty;
                tree.Hierarchy = tree.Hierarchy?
                    .Where(h => !string.IsNullOrWhiteSpace(h))
                    .Select(h => h.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
                    ?? new List<string>();

                cleaned[key] = tree;
            }

            _maps[mapKey] = new HierarchyTreeMapDTO
            {
                HierarchyTrees = cleaned
            };
        }

        public List<string> GetHierarchyForDiscipline(string discipline)
        {
            // === 1️⃣ Normalize discipline ===
            var disc = string.IsNullOrWhiteSpace(discipline)
                ? "DEFAULT"
                : discipline.Trim().ToUpperInvariant();

            // === 2️⃣ Exact discipline match (ST, CI, ME, etc.) ===
            if (_maps[Key].HierarchyTrees.TryGetValue(disc, out var match))
                return match.Hierarchy;

            // === 3️⃣ Fallback to DEFAULT if not defined ===
            if (_maps[Key].HierarchyTrees.TryGetValue("DEFAULT", out var fallback))
                return fallback.Hierarchy;

            // === 4️⃣ No default found — configuration error ===
            throw new InvalidOperationException(
                $"❌ No hierarchy tree defined for discipline '{discipline}' or 'DEFAULT' in HierarchyTreeMap.");
        }

        // Strongly typed (for engines)
        public HierarchyTreeMapDTO GetMap() => _maps[Key];

        // Untyped (for CLI/UI manager)
        public object GetMapUntyped() => GetMap();

        public string GetFilePath() => _configProvider.GetHierarchyTreeMapPath();

        public void Reload() => LoadMap(Key, GetFilePath());
    }
}
