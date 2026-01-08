using Newtonsoft.Json;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.IO;

namespace PlantGateway.Core.Config.Implementations.Maps
{
    /// <summary>
    /// Provides access to the Entity Map JSON configuration file.
    /// </summary>
    public class AllowedTreeMapService : IMapService<AllowedTreeMapDTO>, IAllowedTreeMapService
    {
        private readonly Dictionary<MapKeys, AllowedTreeMapDTO> _maps;
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        public MapKeys Key => MapKeys.AllowedTree;

        public string Description => "...";

        public AllowedTreeMapService(IConfigProvider configProvider, IServiceProvider serviceProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _maps = new Dictionary<MapKeys, AllowedTreeMapDTO>();
            LoadMap(Key, GetFilePath());
        }


        private void LoadMap(MapKeys mapKey, string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"❌ AllowedTree map file not found: {path}");

            var json = File.ReadAllText(path);

            var dto = JsonConvert.DeserializeObject<AllowedTreeMapDTO>(json)
                      ?? throw new InvalidOperationException("❌ Invalid AllowedTreeMap JSON format.");

            _maps[mapKey] = dto;
        }

        public IReadOnlyList<string> GetAllowedChildren(string parentType)
        {
            if (_maps[Key].AllowedTree.TryGetValue(parentType, out var node))
                return node.AllowedChildren;

            return Array.Empty<string>();
        }

        public string GetDescription(string nodeType)
        {
            if (_maps[Key].AllowedTree.TryGetValue(nodeType, out var node))
                return node.Description ?? string.Empty;

            return string.Empty;
        }
        public AllowedTreeMapDTO GetMap() => _maps[Key];
        public object GetMapUntyped() => GetMap();
        public string GetFilePath() => _configProvider.GetAllowedTreeMapPath();
        public void Reload() => LoadMap(Key, GetFilePath());


        /// <summary>
        /// Determines if a given node type can have the specified parent.
        /// </summary>
        public bool IsAllowedParent(string nodeType, string potentialParentType)
        {
            if (string.IsNullOrWhiteSpace(nodeType) || string.IsNullOrWhiteSpace(potentialParentType))
                return false;

            var map = GetMap().AllowedTree;
            foreach (var kvp in map)
            {
                if (kvp.Value.AllowedChildren.Contains(nodeType)
                    && kvp.Key.Equals(potentialParentType, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a given node type can have the specified child.
        /// </summary>
        public bool IsAllowedChild(string nodeType, string potentialChildType)
        {
            if (string.IsNullOrWhiteSpace(nodeType) || string.IsNullOrWhiteSpace(potentialChildType))
                return false;

            var map = GetMap().AllowedTree;
            if (map.TryGetValue(nodeType, out var node))
                return node.AllowedChildren.Contains(potentialChildType);

            return false;
        }
    }
}
