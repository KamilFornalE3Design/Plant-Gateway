using Newtonsoft.Json;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.IO;

namespace PlantGateway.Core.Config.Implementations.Maps
{
    public class CatrefMapService : IMapService<CatrefMapDTO>, ICatrefMapService
    {
        private readonly Dictionary<MapKeys, CatrefMapDTO> _maps;
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        // Implement IMapService
        public MapKeys Key => MapKeys.CATREF;
        public string Description => "Take Over Point Geometry Map to convert input string-like geometry into Aveva-specific Catalogue Reference.";

        public CatrefMapService(IConfigProvider configProvider, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            _maps = new Dictionary<MapKeys, CatrefMapDTO>();

            LoadMap(MapKeys.CATREF, _configProvider.GetTakeOverPointGeometryMapPath());
        }

        private void LoadMap(MapKeys mapKey, string path)
        {
            var json = File.ReadAllText(path);
            var dto = JsonConvert.DeserializeObject<CatrefMapDTO>(json);
            _maps[mapKey] = dto ?? throw new InvalidOperationException("❌ Invalid Catref map JSON");
        }

        // Strongly typed (for engines)
        public CatrefMapDTO GetMap() => _maps[Key];

        // Untyped (for CLI/UI manager)
        public object GetMapUntyped() => GetMap();

        public string GetFilePath() => _configProvider.GetTakeOverPointGeometryMapPath();

        public void Reload() => LoadMap(MapKeys.CATREF, _configProvider.GetTakeOverPointGeometryMapPath());
    }
}
