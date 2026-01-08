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
    /// <summary>
    /// Provides access to the Entity Map JSON configuration file.
    /// </summary>
    public class EntityMapService : IMapService<EntityMapDTO>, IEntityMapService
    {
        private readonly Dictionary<MapKeys, EntityMapDTO> _maps;
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        public MapKeys Key => MapKeys.Entity;

        public string Description =>
            "Defines AVEVA entities (e.g., SDE, NOZ, DATUM) with their type, description, " +
            "and category for consistent import and validation.";

        public EntityMapService(IConfigProvider configProvider, IServiceProvider serviceProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _maps = new Dictionary<MapKeys, EntityMapDTO>();

            LoadMap(Key, GetFilePath());
        }
        private void LoadMap(MapKeys mapKey, string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("❌ Entity map file not found", path);

            var json = File.ReadAllText(path).Trim();

            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException($"❌ Entity map file is empty: {path}");

            // ✅ Deserialize directly into a dictionary of EntityDTOs
            var raw = JsonConvert.DeserializeObject<Dictionary<string, EntityDTO>>(json)
                      ?? throw new InvalidOperationException("❌ Invalid EntityMap JSON format (deserialization returned null).");

            if (raw.Count == 0)
                throw new InvalidOperationException("❌ Entity map JSON contains no entries: " + path);

            // ✅ Normalize and populate DTO
            var dto = new EntityMapDTO
            {
                Entities = raw
                    .Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
                    .ToDictionary(
                        kv => kv.Key.Trim(),
                        kv =>
                        {
                            var e = kv.Value ?? new EntityDTO();
                            e.Code = string.IsNullOrWhiteSpace(e.Code) ? kv.Key.Trim() : e.Code.Trim();
                            e.Name = e.Name?.Trim() ?? string.Empty;
                            e.Location = e.Location?.Trim() ?? string.Empty;
                            return e;
                        },
                        StringComparer.OrdinalIgnoreCase)
            };

            _maps[mapKey] = dto;
        }

        public EntityMapDTO GetMap() => _maps[Key];
        public object GetMapUntyped() => GetMap();
        public string GetFilePath() => _configProvider.GetEntityMapPath();
        public void Reload() => LoadMap(Key, GetFilePath());
    }
}
