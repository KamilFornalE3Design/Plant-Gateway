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
    /// Provides access to the Discipline Map JSON configuration file.
    /// 
    /// The discipline map defines the full taxonomy of engineering disciplines
    /// used within the organization — including their short codes (e.g. "ME", "PO"),
    /// family grouping (Plant, Building, Project Management, etc.), 
    /// official designations, and sorting order for standardized presentation.
    /// 
    /// It acts as the canonical list of disciplines used by Plant Gateway 
    /// for classification, reporting, and automation of mapping logic.
    /// </summary>
    public class DisciplineMapService : IMapService<DisciplineMapDTO>, IDisciplineMapService
    {
        private readonly Dictionary<MapKeys, DisciplineMapDTO> _maps;
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        public MapKeys Key => MapKeys.Discipline;

        public string Description =>
            "Defines all engineering disciplines used by the company, grouped by family " +
            "with consistent codes, designations, and sorting order for classification, " +
            "reporting, and automation in Plant Gateway.";

        public DisciplineMapService(IConfigProvider configProvider, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            _maps = new Dictionary<MapKeys, DisciplineMapDTO>();

            LoadMap(MapKeys.Discipline, _configProvider.GetDisciplinesMapPath());
        }

        private void LoadMap(MapKeys mapKey, string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"❌ Discipline map file not found: {path}");

            var json = File.ReadAllText(path);
            var raw = JsonConvert.DeserializeObject<Dictionary<string, DisciplineDTO>>(json)
                      ?? throw new InvalidOperationException("❌ Invalid DisciplineMap JSON format.");

            var dto = new DisciplineMapDTO
            {
                Disciplines = raw.ToDictionary(
                    kvp => kvp.Key,
                    kvp =>
                    {
                        kvp.Value.Code = kvp.Key; // ensure Code matches the key
                        return kvp.Value;
                    },
                    StringComparer.OrdinalIgnoreCase)
            };

            //ValidateMap(dto);
            _maps[mapKey] = dto;
        }

        /// <summary>
        /// Performs internal validation to detect common issues
        /// such as missing fields, duplicated order numbers, or empty families.
        /// </summary>
        private void ValidateMap(DisciplineMapDTO dto)
        {
            var duplicates = dto.Disciplines.Values
                .GroupBy(d => d.Order)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
                Console.WriteLine($"⚠️ Duplicate discipline order(s): {string.Join(", ", duplicates)}");

            foreach (var d in dto.Disciplines.Values)
            {
                if (string.IsNullOrWhiteSpace(d.Family))
                    Console.WriteLine($"⚠️ Discipline {d.Code} has no Family defined.");
                if (string.IsNullOrWhiteSpace(d.Designation))
                    Console.WriteLine($"⚠️ Discipline {d.Code} has no Designation defined.");
                if (d.Order == int.MinValue)
                    Console.WriteLine($"⚠️ Discipline {d.Code} has no Order defined.");
            }

            if (dto.Disciplines.Count == 0)
                Console.WriteLine("⚠️ Warning: Discipline map is empty.");
        }

        public DisciplineMapDTO GetMap() => _maps[Key];
        public object GetMapUntyped() => GetMap();
        public string GetFilePath() => _configProvider.GetDisciplinesMapPath();
        public void Reload() => LoadMap(Key, _configProvider.GetDisciplinesMapPath());
    }
}
