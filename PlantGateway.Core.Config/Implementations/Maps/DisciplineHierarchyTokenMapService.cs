using Newtonsoft.Json;
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
    public class DisciplineHierarchyTokenMapService : IMapService<DisciplineHierarchyTokenMapDTO>, IDisciplineHierarchyTokenMapService
    {

        private readonly Dictionary<MapKeys, DisciplineHierarchyTokenMapDTO> _maps;
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        public MapKeys Key => MapKeys.DisciplineHierarchyToken;

        public string Description =>
            "Defines AVEVA entities (e.g., SDE, NOZ, DATUM) with their type, description, " +
            "and category for consistent import and validation.";

        public DisciplineHierarchyTokenMapService(IConfigProvider configProvider, IServiceProvider serviceProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _maps = new Dictionary<MapKeys, DisciplineHierarchyTokenMapDTO>();

            LoadMap(Key, GetFilePath());
        }
        private void LoadMap(MapKeys mapKey, string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("❌ DisciplineHierarchyTokenMap file not found", path);

            var json = File.ReadAllText(path).Trim();

            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException($"❌ DisciplineHierarchyTokenMap file is empty: {path}");

            var root = JObject.Parse(json);

            // === 1️⃣ Get disciplines ===
            var disciplines = root.Properties()
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .Select(p => new
                {
                    Name = p.Name.Trim(),
                    Body = (JObject)p.Value
                });

            // === 2️⃣ Get hierarchy per discipline ===
            var disciplineMap = disciplines.ToDictionary(
                d => d.Name,
                d =>
                {
                    var hierarchy = (d.Body["Hierarchy"] as JArray)?
                        .Values<string>()
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .ToList()
                        ?? new List<string>();

                    // === 3️⃣ Get tokens per discipline ===
                    var tokens = (d.Body["Tokens"] as JObject)?
                        .Properties()
                        .Select(t => new
                        {
                            Name = t.Name.Trim(),
                            Value = (JObject)t.Value
                        })
                        .ToDictionary(
                            t => t.Name,
                            t => new TokenGroupDTO
                            {
                                Affix = ((t.Value["Affix"] as JArray)?.Values<string>()
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .Select(x => x.Trim())
                                    .ToList()) ?? new List<string>(),

                                Base = ((t.Value["Base"] as JArray)?.Values<string>()
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .Select(x => x.Trim())
                                    .ToList()) ?? new List<string>(),

                                Suffix = ((t.Value["Suffix"] as JArray)?.Values<string>()
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .Select(x => x.Trim())
                                    .ToList()) ?? new List<string>()
                            },
                            StringComparer.OrdinalIgnoreCase)
                        ?? new Dictionary<string, TokenGroupDTO>(StringComparer.OrdinalIgnoreCase);

                    return new DisciplineDefinitionDTO
                    {
                        Hierarchy = hierarchy,
                        Tokens = tokens
                    };
                },
                StringComparer.OrdinalIgnoreCase);

            _maps[mapKey] = new DisciplineHierarchyTokenMapDTO
            {
                Disciplines = disciplineMap
            };
        }

        public List<string> GetHierarchyForDiscipline(string discipline)
        {
            // === 1️⃣ Normalize discipline ===
            var disc = string.IsNullOrWhiteSpace(discipline)
                ? "DEFAULT"
                : discipline.Trim().ToUpperInvariant();

            // === 2️⃣ Exact discipline match (ST, CI, ME, etc.) ===
            if (_maps[Key].Disciplines.TryGetValue(disc, out var definition)
                && definition?.Hierarchy?.Any() == true)
            {
                return definition.Hierarchy;
            }

            // === 3️⃣ Fallback to DEFAULT if not defined ===
            if (_maps[Key].Disciplines.TryGetValue("DEFAULT", out var fallback)
                && fallback?.Hierarchy?.Any() == true)
            {
                return fallback.Hierarchy;
            }

            // === 4️⃣ No default found — configuration error ===
            throw new InvalidOperationException(
                $"❌ No hierarchy defined for discipline '{discipline}' or 'DEFAULT' in DisciplineHierarchyTokenMap.");
        }

        /// <summary>
        /// Returns the token list for the given node type (e.g. SITE, ZONE).
        /// Used for base-name generation in hierarchy.
        /// </summary>
        public List<string> GetTokens(string discipline, string tokenGroupKey)
        {
            if (string.IsNullOrWhiteSpace(discipline) || string.IsNullOrWhiteSpace(tokenGroupKey))
                return new List<string>();

            var disc = discipline.Trim().ToUpperInvariant();
            var tokenKey = tokenGroupKey.Trim();

            if (_maps[Key].Disciplines.TryGetValue(disc, out var definition) &&
                definition.Tokens.TryGetValue(tokenKey, out var tokenGroup))
            {
                // Combine all token components (Affix + Base + Suffix)
                return tokenGroup.Affix
                    .Concat(tokenGroup.Base)
                    .Concat(tokenGroup.Suffix)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
            }

            // === fallback to DEFAULT discipline if not found ===
            if (_maps[Key].Disciplines.TryGetValue("DEFAULT", out var fallback) &&
                fallback.Tokens.TryGetValue(tokenKey, out var fallbackGroup))
            {
                return fallbackGroup.Affix
                    .Concat(fallbackGroup.Base)
                    .Concat(fallbackGroup.Suffix)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
            }

            return new List<string>();
        }

        public DisciplineHierarchyTokenMapDTO GetMap() => _maps[Key];
        public object GetMapUntyped() => GetMap();
        public string GetFilePath() => _configProvider.GetDisciplineHierarchyTokenMapPath();
        public void Reload() => LoadMap(Key, GetFilePath());
    }
}
