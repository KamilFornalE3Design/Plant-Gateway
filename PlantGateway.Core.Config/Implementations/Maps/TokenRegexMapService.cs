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
    public class TokenRegexMapService : IMapService<TokenRegexMapDTO>, ITokenRegexMapService
    {
        private readonly Dictionary<MapKeys, TokenRegexMapDTO> _maps;
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        // === Cached lookups ===
        private Dictionary<int, string> _defaultNameOrder = new Dictionary<int, string>();           // Position → Token name
        private Dictionary<int, TokenRegexDTO> _tokenByPosition = new Dictionary<int, TokenRegexDTO>();     // Position → DTO

        private readonly object _syncRoot = new object();

        // Implement IMapService
        public MapKeys Key => MapKeys.TokenRegex;
        public string Description => "";

        public TokenRegexMapService(IConfigProvider configProvider, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            _maps = new Dictionary<MapKeys, TokenRegexMapDTO>();

            LoadMap(Key, GetFilePath());
            BuildCachedLookups();
        }

        private void LoadMap(MapKeys mapKey, string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("❌ TokenRegexMap file not found", path);

            var json = File.ReadAllText(path);
            var dto = JsonConvert.DeserializeObject<TokenRegexMapDTO>(json)
                ?? throw new InvalidOperationException("❌ Invalid TokenRegexMap JSON");

            // 🔹 Inject Name = JSON key for each token
            foreach (var kv in dto.TokenRegex)
            {
                if (kv.Value != null)
                    kv.Value.Name = kv.Key;
            }

            _maps[mapKey] = dto;
        }

        /// <summary>
        /// Builds in-memory lookup dictionaries (position-based tokens and order chain).
        /// Called during initialization and Reload().
        /// </summary>
        private void BuildCachedLookups()
        {
            lock (_syncRoot)
            {
                var tokenMap = _maps[Key].TokenRegex;

                _tokenByPosition = tokenMap
                    .Where(kv => kv.Value.Position >= 0)
                    .GroupBy(kv => kv.Value.Position)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().Value);

                _defaultNameOrder = tokenMap
                    .Where(kv => kv.Value.Type.Equals("base", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(kv => kv.Value.Position)
                    .ToDictionary(
                        kv => kv.Value.Position,
                        kv => kv.Key,
                        EqualityComparer<int>.Default);

                // Ensure default position 4 (Component) exists even if not defined
                if (!_defaultNameOrder.ContainsKey(4))
                    _defaultNameOrder[4] = "Component";
            }
        }

        /// <summary>
        /// Returns the TokenRegexDTO that defines a given position.
        /// </summary>
        public TokenRegexDTO GetTokenForPosition(int position)
        {
            lock (_syncRoot)
                return _tokenByPosition.TryGetValue(position, out var token) ? token : null;
        }

        /// <summary>
        /// Returns the ordered list of default naming tokens (Plant→Unit→Section→Equipment→Component).
        /// </summary>
        public IReadOnlyList<string> GetDefaultNameOrder()
        {
            lock (_syncRoot)
                return _defaultNameOrder.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
        }

        public int GetHighestBasePosition()
        {
            return _maps[Key].TokenRegex
                .Where(kv => kv.Value.Type.Equals("base", StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Value.Position)
                .DefaultIfEmpty(-1)
                .Max();
        }

        // Strongly typed (for engines)
        public TokenRegexMapDTO GetMap() => _maps[Key];

        // Untyped (for CLI/UI manager)
        public object GetMapUntyped() => GetMap();

        public string GetFilePath() => _configProvider.GetTokenRegexMapPath();

        /// <summary>
        /// Reloads JSON and rebuilds cached lookup tables.
        /// </summary>
        public void Reload()
        {
            LoadMap(Key, GetFilePath());
            BuildCachedLookups();
        }
    }
}
