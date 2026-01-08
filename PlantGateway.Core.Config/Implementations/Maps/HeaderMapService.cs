using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.IO;

namespace PlantGateway.Core.Config.Implementations.Maps
{
    public class HeaderMapService : IMapService<HeaderMapDTO>, IHeaderMapService
    {
        private readonly Dictionary<MapKeys, HeaderMapDTO> _maps;
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        // Implement IMapService
        public MapKeys Key => MapKeys.TOP;
        public string Description => "Take Over Point Text file input headers used to map incomming headers to DTO Properties.";


        public HeaderMapService(IConfigProvider configProvider, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            _maps = new Dictionary<MapKeys, HeaderMapDTO>();

            LoadMap(MapKeys.TOP, _configProvider.GetTakeOverPointHeaderMapPath());
        }

        private void LoadMap(MapKeys mapKey, string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("❌ Header map file not found", path);

            var json = File.ReadAllText(path);
            var dto = Newtonsoft.Json.JsonConvert.DeserializeObject<HeaderMapDTO>(json);
            if (dto == null)
                throw new InvalidOperationException("❌ Header map JSON invalid: " + path);

            _maps[mapKey] = dto;
        }

        public string TryMap(MapKeys mapKey, string rawHeader)
        {
            if (!_maps.ContainsKey(mapKey))
                throw new KeyNotFoundException("❌ Map not loaded: " + mapKey);

            var map = _maps[mapKey];
            return map.Headings.TryGetValue(rawHeader.Trim(), out var mapped) ? mapped : string.Empty;
        }

        public string[] GetGroupMembers(MapKeys mapKey, string groupName)
        {
            if (!_maps.ContainsKey(mapKey))
                throw new KeyNotFoundException("❌ Map not loaded: " + mapKey);

            var map = _maps[mapKey];
            return map.Groups.TryGetValue(groupName, out var members)
                ? members
                : Array.Empty<string>();
        }

        public HeaderMapDTO GetMap(MapKeys mapKey)
        {
            if (!_maps.ContainsKey(mapKey))
                throw new KeyNotFoundException("❌ Map not loaded: " + mapKey);

            return _maps[mapKey];
        }

        public TDto MapRow<TDto>(MapKeys mapKey, string line, string[] rawHeaders) where TDto : class
        {
            var mapper = _serviceProvider.GetService(typeof(IRowMapper<TDto>)) as IRowMapper<TDto>;
            if (mapper == null)
                throw new NotSupportedException($"❌ No row mapper registered for {typeof(TDto).Name}");

            return mapper.MapRow(line, rawHeaders, GetMap(mapKey));
        }

        // Strongly typed (for engines)
        public HeaderMapDTO GetMap() => _maps[Key];

        // Untyped (for CLI/UI manager)
        public object GetMapUntyped() => GetMap();

        public string GetFilePath() => _configProvider.GetTakeOverPointHeaderMapPath();

        public void Reload() => LoadMap(Key, _configProvider.GetTakeOverPointHeaderMapPath());

    }
}
