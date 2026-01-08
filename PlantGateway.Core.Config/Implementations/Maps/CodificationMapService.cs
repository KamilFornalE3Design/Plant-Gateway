using Newtonsoft.Json.Linq;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PlantGateway.Core.Config.Implementations.Maps
{
    /// <summary>
    /// Loads and manages the codification hierarchy map (Plant → Unit → Section → Equipment).
    /// Used to identify structure level of codes and provide quick lookups.
    /// </summary>
    public class CodificationMapService : IMapService<CodificationMapDTO>, ICodificationMapService
    {
        private readonly Dictionary<MapKeys, CodificationMapDTO> _maps;
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        public MapKeys Key => MapKeys.Codification;

        public string Description =>
            "Defines the Plant Breakdown Structure codification tree (Plant → Unit → Section → Equipment).";

        public CodificationMapService(IConfigProvider configProvider, IServiceProvider serviceProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _maps = new Dictionary<MapKeys, CodificationMapDTO>();
            LoadMap(Key, GetFilePath());
        }

        public CodificationMapDTO GetMap() => _maps[Key];
        public object GetMapUntyped() => GetMap();
        public string GetFilePath() => _configProvider.GetCodificationMapPath();
        public void Reload() => LoadMap(Key, GetFilePath());


        // ============================================================
        // ===  PUBLIC ACCESS POINTS  ================================
        // ============================================================

        public bool IsKnownPlant(string inputCode) =>
            ExecuteIsKnown(inputCode, CodificationType.Plant);

        public bool IsKnownPlantUnit(string inputCode) =>
            ExecuteIsKnown(inputCode, CodificationType.PlantUnit);

        public bool IsKnownPlantSection(string inputCode) =>
            ExecuteIsKnown(inputCode, CodificationType.PlantSection);

        public bool IsKnownEquipment(string inputCode) =>
            ExecuteIsKnown(inputCode, CodificationType.Equipment);

        public CodificationType GetCodificationType(string inputCode) =>
            ExecuteGetType(inputCode);

        public List<string> GetKnownPlants() =>
            ExecuteGetKnown(CodificationType.Plant);

        public List<string> GetKnownPlantUnits(string plantCode) =>
            ExecuteGetChildren(plantCode, CodificationType.PlantUnit);

        public List<string> GetKnownPlantSections(string plantUnitCode) =>
            ExecuteGetChildren(plantUnitCode, CodificationType.PlantSection);

        public List<string> GetKnownEquipment(string plantSectionCode) =>
            ExecuteGetChildren(plantSectionCode, CodificationType.Equipment);

        // ============================================================
        // ===  PRIVATE EXECUTION HELPERS  ============================
        // ============================================================

        private bool ExecuteIsKnown(string code, CodificationType expectedType)
        {
            var key = Normalize(code);
            return GetMap().Codifications.TryGetValue(key, out var dto)
                   && dto.CodificationType == expectedType;
        }

        private CodificationType ExecuteGetType(string code)
        {
            var key = Normalize(code);
            return GetMap().Codifications.TryGetValue(key, out var dto)
                ? dto.CodificationType
                : CodificationType.Undefined;
        }

        private List<string> ExecuteGetKnown(CodificationType type)
        {
            return GetMap().Codifications
                .Where(x => x.Value.CodificationType == type)
                .Select(x => x.Key)
                .OrderBy(x => x)
                .ToList();
        }

        private List<string> ExecuteGetChildren(string parentCode, CodificationType expectedChildType)
        {
            var map = GetMap();
            var key = Normalize(parentCode);

            if (!map.Codifications.TryGetValue(key, out var parent))
                return new List<string>();

            return parent.Children
                .Where(child => map.Codifications[child].CodificationType == expectedChildType)
                .OrderBy(x => x)
                .ToList();
        }

        private static string Normalize(string input) =>
            input?.Trim().ToUpperInvariant() ?? string.Empty;

        private void LoadMap(MapKeys mapKey, string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("❌ CodificationMap file not found", path);

            var json = File.ReadAllText(path).Trim();
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException($"❌ CodificationMap file is empty: {path}");

            var root = JObject.Parse(json);
            var map = new CodificationMapDTO();

            foreach (var plantProp in root.Properties())
            {
                var plant = plantProp.Name.Trim();
                AddCodification(map, plant, CodificationType.Plant);

                var units = plantProp.Value as JObject;
                if (units == null) continue;

                foreach (var unitProp in units.Properties())
                {
                    var unit = unitProp.Name.Trim();
                    AddCodification(map, unit, CodificationType.PlantUnit, plant);
                    map.Codifications[plant].AddChild(unit);

                    var sections = unitProp.Value as JObject;
                    if (sections == null) continue;

                    foreach (var sectionProp in sections.Properties())
                    {
                        var section = sectionProp.Name.Trim();
                        AddCodification(map, section, CodificationType.PlantSection, unit);
                        map.Codifications[unit].AddChild(section);

                        var eqArray = sectionProp.Value as JArray;
                        if (eqArray == null) continue;

                        foreach (var eq in eqArray.Values<string>())
                        {
                            if (string.IsNullOrWhiteSpace(eq)) continue;
                            var eqCode = eq.Trim();

                            AddCodification(map, eqCode, CodificationType.Equipment, section);
                            map.Codifications[section].AddChild(eqCode);
                        }
                    }
                }
            }

            _maps[mapKey] = map;
        }

        private static void AddCodification(CodificationMapDTO map, string code, CodificationType type, string parent = "")
        {
            code = Normalize(code);
            if (string.IsNullOrWhiteSpace(code))
                return;

            if (!map.Codifications.ContainsKey(code))
            {
                map.Add(code, new CodificationDTO(code));
                map.Codifications[code].SetType(type);
                map.Codifications[code].SetParent(parent);
            }
        }
    }
}
