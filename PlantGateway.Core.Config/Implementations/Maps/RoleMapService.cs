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
    public class RoleMapService : IMapService<RoleMapDTO>, IRoleMapService
    {
        private readonly Dictionary<MapKeys, RoleMapDTO> _maps;
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        // Implement IMapService
        public MapKeys Key => MapKeys.Role;
        public string Description => "";

        public RoleMapService(IConfigProvider configProvider, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            _maps = new Dictionary<MapKeys, RoleMapDTO>();

            Reload();
        }

        private void LoadMap(MapKeys mapKey, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path), "❌ Role map path not provided.");

            if (!File.Exists(path))
                throw new FileNotFoundException("❌ Role map file not found.", path);

            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    throw new InvalidOperationException("❌ Role map JSON is empty.");

                // Deserialize into a dictionary directly
                var root = JsonConvert.DeserializeObject<RoleMapDTO>(json);
                if (root?.Roles == null || root.Roles.Count == 0)
                    throw new InvalidOperationException("❌ Role map JSON invalid or missing 'Roles' section.");

                // Normalize and validate each entry
                var validRoles = root.Roles
                    .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
                    .ToDictionary(
                        kvp => kvp.Key.Trim(),
                        kvp => new RoleDTO
                        {
                            AvevaType = kvp.Key.Trim(),
                            BusinessConcept = kvp.Value.BusinessConcept?.Trim() ?? string.Empty,
                            IsLeaf = kvp.Value.IsLeaf,
                            DisciplineGroups = kvp.Value.DisciplineGroups ?? new List<string>(),
                            Description = kvp.Value.Description?.Trim() ?? string.Empty
                        },
                        StringComparer.OrdinalIgnoreCase);

                var dto = new RoleMapDTO { Roles = validRoles };
                _maps[mapKey] = dto;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"❌ Failed to load Role map from '{path}': {ex.Message}", ex);
            }
        }


        // Strongly typed (for engines)
        public RoleMapDTO GetMap() => _maps[Key];

        // Untyped (for CLI/UI manager)
        public object GetMapUntyped() => GetMap();

        public string GetFilePath() => _configProvider.GetRoleMapPath();

        public void Reload() => LoadMap(Key, GetFilePath());
    }
}
