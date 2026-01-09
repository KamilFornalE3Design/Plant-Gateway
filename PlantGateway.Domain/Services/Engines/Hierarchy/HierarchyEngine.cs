using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Utilities.Helpers;
using System.Data;
using System.Xml;
using System.Xml.Linq;

namespace SMSgroup.Aveva.Utilities.Engines
{
    public sealed class HierarchyEngine : IEngine
    {
        private readonly IDisciplineHierarchyTokenMapService _disciplineHierarchyTokenMapService;
        private readonly IAllowedTreeMapService _allowedTreeService;

        private readonly List<string> _localLog = new();

        public HierarchyEngine(IDisciplineHierarchyTokenMapService disciplineHierarchyTokenMapService, IAllowedTreeMapService allowedTreeService)
        {
            _disciplineHierarchyTokenMapService = disciplineHierarchyTokenMapService ?? throw new ArgumentNullException(nameof(disciplineHierarchyTokenMapService));
            _allowedTreeService = allowedTreeService ?? throw new ArgumentNullException(nameof(allowedTreeService));
        }

        public HierarchyEngineResult Process(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract) => new HierarchyEngineResult();

        public HierarchyEngineResult Process(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var result = new HierarchyEngineResult
            {
                SourceDtoId = dto.Id,
                HierarchyChain = new List<HierarchyNode>(),
                Message = new List<string>(),
                Warning = new List<string>(),
                Error = new List<string>(),
                IsValid = false,
                IsConsistent = true // set true for now; add structural checks later if needed
            };

            var tokenResult = dto.EngineResults?.OfType<TokenEngineResult>().FirstOrDefault();
            if (tokenResult == null || tokenResult.Tokens == null || tokenResult.Tokens.Count == 0)
            {
                result.AddError("❌ No TokenEngineResult or empty tokens; cannot build hierarchy.");
                return result;
            }

            // === Resolve discipline (default = "ME") and load map ===
            var disc = ResolveDisciplineOrDefault(tokenResult); // "ME" if missing
            var map = _disciplineHierarchyTokenMapService.GetMap();
            if (map?.Disciplines == null || map.Disciplines.Count == 0)
            {
                result.AddError("❌ DisciplineHierarchy map is empty.");
                return result;
            }

            if (!map.Disciplines.TryGetValue(disc, out var discDef))
            {
                // fallback to DEFAULT if discipline not present
                if (!map.Disciplines.TryGetValue("DEFAULT", out discDef))
                {
                    result.AddError($"❌ Discipline '{disc}' and DEFAULT not found in map.");
                    return result;
                }
                result.AddWarning($"⚠️ Discipline '{disc}' not found; using DEFAULT.");
            }

            // === Ensure leaf is EQUI (no STRU now)
            NormalizeLeafToEqui(discDef);

            // We also look up DEFAULT role specs for any missing roles in the chosen discipline
            var defaultDef = map.Disciplines.TryGetValue("DEFAULT", out var dd) ? dd : discDef;

            // === Build the full chain; only EQUI is non-virtual (leaf import target)
            var nodes = new List<HierarchyNode>();
            string parentTag = string.Empty;

            foreach (var role in discDef.Hierarchy)
            {
                var spec = TryGetRoleSpec(discDef, defaultDef, role, out bool fromDefault);

                var (tag, buildMsg) = BuildRoleTag(spec, tokenResult, role);
                if (!string.IsNullOrWhiteSpace(buildMsg))
                    result.AddMessage(buildMsg + (fromDefault ? " (role spec from DEFAULT)" : string.Empty));

                var isLeaf = role.Equals("EQUI", StringComparison.OrdinalIgnoreCase);

                var node = new HierarchyNode
                {
                    Id = isLeaf ? dto.Id : Guid.Empty,
                    AvevaType = role,
                    AvevaTag = tag,
                    ParentAvevaTag = parentTag,
                    ParentId = null, // consolidator can fill actual parent Guid if it finds a real match
                    Depth = nodes.Count,
                    IsVirtual = !isLeaf,
                    IsConsistent = true
                };

                nodes.Add(node);
                parentTag = tag;
            }

            // Fill result
            result.HierarchyChain = nodes;
            result.Role = "EQUI";                       // leaf role is EQUI (normalized)
            result.AvevaTag = nodes.LastOrDefault()?.AvevaTag ?? string.Empty; // leaf tag
            result.IsValid = true;
            result.AddMessage($"✅ Component hierarchy built for discipline '{disc}' with {nodes.Count} levels.");

            return result;
        }


        // ===== Helpers =====

        // Discipline default = "ME"
        private static string ResolveDisciplineOrDefault(TokenEngineResult tr)
        {
            if (tr?.Tokens != null &&
                tr.Tokens.TryGetValue("Discipline", out var tok) &&
                !string.IsNullOrWhiteSpace(tok?.Value))
            {
                return tok.Value.Trim().ToUpperInvariant();
            }
            return "ME";
        }

        // Entity default = "SDE" (used when suffix asks for it and token missing)
        private static string ResolveEntityOrDefault(TokenEngineResult tr)
        {
            if (tr?.Tokens != null &&
                tr.Tokens.TryGetValue("Entity", out var tok) &&
                !string.IsNullOrWhiteSpace(tok?.Value))
            {
                return tok.Value.Trim().ToUpperInvariant();
            }
            return "SDE";
        }

        private static void NormalizeLeafToEqui(DisciplineDefinitionDTO def)
        {
            if (def?.Hierarchy == null || def.Hierarchy.Count == 0) return;
            for (int i = 0; i < def.Hierarchy.Count; i++)
            {
                if (def.Hierarchy[i].Equals("STRU", StringComparison.OrdinalIgnoreCase))
                    def.Hierarchy[i] = "EQUI";
            }
        }

        private static TokenGroupDTO TryGetRoleSpec(
            DisciplineDefinitionDTO discDef,
            DisciplineDefinitionDTO defaultDef,
            string role,
            out bool fromDefault)
        {
            fromDefault = false;

            if (discDef?.Tokens != null && discDef.Tokens.TryGetValue(role, out var own))
                return own;

            if (defaultDef?.Tokens != null && defaultDef.Tokens.TryGetValue(role, out var def))
            {
                fromDefault = true;
                return def;
            }

            // As a last resort, synthesize a minimal spec by role name using DEFAULT pattern
            return new TokenGroupDTO
            {
                Affix = new List<string>(),
                Base = role.ToUpperInvariant() switch
                {
                    "WORL" => new List<string> { "Plant" },
                    "SITE" => new List<string> { "Plant", "PlantUnit" },
                    "SUB_SITE" => new List<string> { "Plant", "PlantUnit", "PlantSection" },
                    "ZONE" => new List<string> { "Plant", "PlantUnit", "PlantSection", "Equipment" },
                    _ => new List<string> { "Plant", "PlantUnit", "PlantSection", "Equipment", "Component" } // EQUI
                },
                Suffix = new List<string>()
            };
        }

        /// <summary>
        /// Build role tag using map-driven Base & Suffix, and SeparatorHelper for joining.
        /// Missing base parts become MISSING_{BASEKEY}; missing Discipline/Entity default to ME/SDE.
        /// </summary>
        private static (string Tag, string Message) BuildRoleTag(
            TokenGroupDTO spec,
            TokenEngineResult tr,
            string role)
        {
            var messages = new List<string>();
            var baseParts = new List<string>();

            // Collect base parts
            foreach (var baseKey in spec.Base ?? Enumerable.Empty<string>())
            {
                if (tr?.Tokens != null && tr.Tokens.TryGetValue(baseKey, out var tok) && !string.IsNullOrWhiteSpace(tok?.Value))
                {
                    baseParts.Add(tok.Value);
                }
                else
                {
                    var missing = $"MISSING_{baseKey.ToUpperInvariant()}";
                    baseParts.Add(missing);
                    messages.Add($"ℹ️ '{role}' missing base '{baseKey}', using '{missing}'.");
                }
            }

            // Structural join of base parts
            // If your SeparatorHelper has a different API, adapt these two calls accordingly:
            var tagCore = SeparatorHelper.JoinAuto(baseParts.ToArray());

            // Append suffixes via SeparatorHelper.JoinWith(suffixKey, tagCore, suffixValue)
            foreach (var sfxKey in spec.Suffix ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(sfxKey))
                    continue;

                string value = null;

                if (tr?.Tokens != null && tr.Tokens.TryGetValue(sfxKey, out var tok) && !string.IsNullOrWhiteSpace(tok?.Value))
                    value = tok.Value.Trim().ToUpperInvariant();
                else
                {
                    // Defaults for known suffixes from DEFAULT map
                    if (sfxKey.Equals("Discipline", StringComparison.OrdinalIgnoreCase))
                        value = "ME";
                    else if (sfxKey.Equals("Entity", StringComparison.OrdinalIgnoreCase))
                        value = "SDE";

                    if (!string.IsNullOrWhiteSpace(value))
                        messages.Add($"ℹ️ '{role}' missing suffix '{sfxKey}', defaulting to '{value}'.");
                }

                if (!string.IsNullOrWhiteSpace(value))
                    tagCore = SeparatorHelper.JoinWith(sfxKey, tagCore, value);
            }

            return ("/" + tagCore, messages.Count > 0 ? string.Join(" ", messages) : string.Empty);
        }

        private static string MissingSuffixDefault(string suffixKey, out string message)
        {
            // Only Discipline/Entity are expected in DEFAULT map Suffix; default them if missing.
            if (suffixKey.Equals("Discipline", StringComparison.OrdinalIgnoreCase))
            {
                message = "ℹ️ Discipline missing; defaulting to 'ME'.";
                return "ME";
            }
            if (suffixKey.Equals("Entity", StringComparison.OrdinalIgnoreCase))
            {
                message = "ℹ️ Entity missing; defaulting to 'SDE'.";
                return "SDE";
            }
            message = string.Empty;
            return string.Empty;
        }

        private List<string> BuildEffectiveHierarchy(ProjectStructureDTO dto, string contextKey)
        {
            var map = _disciplineHierarchyTokenMapService.GetMap();
            if (!map.Disciplines.TryGetValue(contextKey, out var disciplineConfig))
                disciplineConfig = map.Disciplines["DEFAULT"];

            var hierarchy = disciplineConfig.Hierarchy.ToList();

            // Trim at current role level if defined
            var roleResult = dto.EngineResults.OfType<RoleEngineResult>().FirstOrDefault();
            var role = roleResult?.AvevaType?.Trim().ToUpperInvariant() ?? string.Empty;

            if (!string.IsNullOrEmpty(role))
            {
                int idx = hierarchy.FindIndex(h => h.Equals(role, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                    hierarchy = hierarchy.Take(idx + 1).ToList();
            }

            return hierarchy;
        }

        private Dictionary<string, string> BuildHierarchyTags(ProjectStructureDTO dto, string contextKey, List<string> hierarchy)
        {
            var map = _disciplineHierarchyTokenMapService.GetMap();
            if (!map.Disciplines.TryGetValue(contextKey, out var disciplineConfig))
                disciplineConfig = map.Disciplines["DEFAULT"];

            var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Generate tag names based on hierarchy level order
            foreach (var level in hierarchy)
            {
                // Placeholder: later you may replace this with actual tag builder logic
                var tagValue = $"{dto.EngineResults.OfType<TokenEngineResult>().FirstOrDefault()?.NormalizedInputValue ?? "UNDEF"}_{level}";
                tags[level] = tagValue;
            }

            dto.EngineResults.OfType<HierarchyEngineResult>().FirstOrDefault()?
                .AddMessage($"🏷 Built {tags.Count} hierarchy tags for context '{contextKey}'.Levels → {string.Join(", ", tags.Keys)}");

            return tags;
        }

        private static string DetectContext(TokenEngineResult tokenResult, string discipline, IEngineResult result)
        {
            if (tokenResult == null)
                return "DEFAULT";

            // === 1️⃣ Safe replacement check ===
            bool isPlantSectionReplaced = tokenResult.Tokens.Values.Any(t =>
                t.Position == 2 &&
                !string.IsNullOrEmpty(t.ReplacesKey) &&
                t.ReplacesKey.Equals("PlantSection", StringComparison.OrdinalIgnoreCase));

            if (isPlantSectionReplaced)
            {
                result?.AddMessage("🏗 Context detected: PlantSection replaced (pos=2) → LA hierarchy.");
                return "LA";
            }

            // === 2️⃣ Normalize discipline ===
            discipline = discipline?.Trim().ToUpperInvariant() ?? string.Empty;

            // === 3️⃣ Determine hierarchy key ===
            string selectedKey = discipline switch
            {
                "LA" => "LA",
                "ST" => "ST",
                "CI" => "CI",
                _ => "DEFAULT"
            };

            // === 4️⃣ Safe logging ===
            result?.AddMessage(selectedKey switch
            {
                "LA" => "🏗 Context detected: Layout discipline (LA) → LA hierarchy.",
                "ST" => "🏗 Context detected: Structural discipline (ST) → ST hierarchy.",
                "CI" => "🏗 Context detected: Civil discipline (CI) → CI hierarchy.",
                _ => "🏗 Context fallback: DEFAULT hierarchy applied."
            });

            return selectedKey;
        }

        /// <summary>
        /// Builds the ordered hierarchy node chain for the given DTO,
        /// using hierarchy levels, resolved tags, and the current role.
        /// Handles STRU ↔ EQUI equivalence and marks all other nodes as virtual.
        /// </summary>
        private static List<HierarchyNode> BuildHierarchyNodeChain(ProjectStructureDTO dto, List<string> hierarchy, Dictionary<string, string> tags, string role)
        {
            if (hierarchy == null || hierarchy.Count == 0)
                throw new ArgumentException("Hierarchy levels cannot be null or empty.", nameof(hierarchy));

            if (tags == null || tags.Count == 0)
                throw new ArgumentException("Tags dictionary cannot be null or empty.", nameof(tags));

            role = role?.Trim().ToUpperInvariant() ?? string.Empty;

            var chain = hierarchy
                .Select((level, index) =>
                {
                    // === 🧠 STRU ↔ EQUI equivalence handling ===
                    // Aveva does not distinguish between STRU and EQUI for import hierarchy.
                    // Both are treated as identical until Tekla or MSCAD integration is complete.
                    bool isEquivalentRole =
                        level.Equals(role, StringComparison.OrdinalIgnoreCase) ||
                        (role.Equals("STRU", StringComparison.OrdinalIgnoreCase) && level.Equals("EQUI", StringComparison.OrdinalIgnoreCase)) ||
                        (role.Equals("EQUI", StringComparison.OrdinalIgnoreCase) && level.Equals("STRU", StringComparison.OrdinalIgnoreCase));

                    // === 🧱 Build hierarchy node ===
                    return new HierarchyNode
                    {
                        // Non-virtual nodes represent the real role or equivalent
                        Id = isEquivalentRole ? dto.Id : Guid.Empty,
                        AvevaType = level,

                        // Use tag if available, otherwise fallback to placeholder
                        AvevaTag = tags.TryGetValue(level, out var tagValue)
                            ? tagValue
                            : $"{level}_UNDEF",

                        ParentAvevaTag = index > 0 && tags.TryGetValue(hierarchy[index - 1], out var parentTag)
                            ? parentTag
                            : string.Empty,

                        Depth = index,
                        IsVirtual = !isEquivalentRole,
                        IsConsistent = true,
                        Children = new List<HierarchyNode>()
                    };
                })
                .ToList();

            return chain;
        }

        private HierarchyNode CreateHierarchyNode(string nodeType, string avevaTag, bool isVirtual, ProjectStructureDTO? dto = null)
        {
            return new HierarchyNode
            {
                Id = !isVirtual && dto != null ? dto.Id : Guid.Empty,
                AvevaType = nodeType,
                AvevaTag = avevaTag,
                IsVirtual = isVirtual,
                Children = new List<HierarchyNode>()
            };
        }

        private string GetExpectedAvevaTag(ProjectStructureDTO dto, string nodeType)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // === 1️⃣ Prerequisites ===
            var tokenResult = dto.EngineResults.OfType<TokenEngineResult>().FirstOrDefault();
            if (tokenResult == null)
                return "MISSING_TOKEN_RESULT";

            var disciplineResult = dto.EngineResults.OfType<DisciplineEngineResult>().FirstOrDefault();
            var entityResult = dto.EngineResults.OfType<EntityEngineResult>().FirstOrDefault();
            var suffixResult = dto.EngineResults.OfType<SuffixEngineResult>().FirstOrDefault();

            var discipline = disciplineResult?.Discipline?.Trim().ToUpperInvariant() ?? "DEFAULT";
            var entity = entityResult?.Entity?.Trim() ?? string.Empty;
            var tagSuffix = suffixResult?.Suffix?.Trim() ?? string.Empty;

            // === 2️⃣ Load discipline-aware map ===
            var map = _disciplineHierarchyTokenMapService.GetMap().Disciplines;
            var disciplineDef = map.TryGetValue(discipline, out var def) ? def : map["DEFAULT"];

            // === 3️⃣ Build base part ===
            if (!disciplineDef.Tokens.TryGetValue(nodeType, out var tokenGroup))
                tokenGroup = map["DEFAULT"].Tokens[nodeType];

            var baseTokens = tokenGroup.Base ?? new List<string>();
            var baseParts = baseTokens;
            // nie wiem jak to rozwiązać na szybko. do poprawki!!!
            //.Select(t => tokenResult.Tokens.TryGetValue(t, out var val) && !string.IsNullOrWhiteSpace(val)
            //    ? val : $"MISSING_{t}")
            //.ToList();

            var baseName = string.Join(".", baseParts);

            // === 4️⃣ Build suffix part ===
            var suffixTokens = tokenGroup.Suffix ?? new List<string>();
            var suffixParts = new List<string>();

            foreach (var token in suffixTokens)
            {
                if (tokenResult.Tokens.TryGetValue(token, out var tokenValue) && !string.IsNullOrWhiteSpace(tokenValue.Key))
                {
                    suffixParts.Add(tokenValue.Key);
                    continue;
                }

                switch (token)
                {
                    case "Discipline":
                        if (!string.IsNullOrWhiteSpace(discipline))
                            suffixParts.Add(discipline);
                        break;

                    case "Entity":
                        if (!string.IsNullOrWhiteSpace(entity))
                            suffixParts.Add(entity);
                        break;

                    case "TagComposite":
                    case "Mechanical":
                    case "Electrical":
                    case "Structural":
                        if (!string.IsNullOrWhiteSpace(tagSuffix))
                            suffixParts.Add(tagSuffix);
                        break;

                    default:
                        _localLog.Add($"⚠️ Unrecognized suffix token '{token}' for {nodeType}");
                        break;
                }
            }

            // === 5️⃣ Combine safely ===
            if (suffixParts.Count > 0)
            {
                var distinctSuffix = suffixParts
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (distinctSuffix.Count > 0)
                {
                    var suffix = string.Join("_", distinctSuffix);
                    return suffix.StartsWith("-") || suffix.StartsWith("_") || suffix.StartsWith(".")
                        ? $"{baseName}{suffix}"
                        : $"{baseName}-{suffix}";
                }
            }

            return baseName;
        }

        private bool ExistsInInput(ProjectStructureDTO dto, string hierarchyTag)
        {
            if (dto == null || string.IsNullOrWhiteSpace(hierarchyTag))
                return false;

            var tokenResult = dto.EngineResults.OfType<TokenEngineResult>().FirstOrDefault();
            if (tokenResult == null || tokenResult.Tokens == null || tokenResult.Tokens.Count == 0)
                return false;

            // Normalize and compare parts
            var normalizedTag = hierarchyTag.Replace("-", "_").Replace(".", "_").ToUpperInvariant();
            var tagSegments = normalizedTag.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

            var tokenValues = tokenResult.Tokens.Values
                .Where(v => !string.IsNullOrWhiteSpace(v.Key))
                .Select(v => v.Key.Trim().ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Check if all tag segments are known tokens
            bool exists = tagSegments.All(seg => tokenValues.Contains(seg));
            return exists;
        }

        private void AppendHierarchyXml(HierarchyEngineResult result, string inputTarget, bool append = false)
        {
            if (result == null || result.HierarchyChain == null || result.HierarchyChain.Count == 0)
                return;

            try
            {
                var dir = Path.GetDirectoryName(inputTarget)!;
                var file = Path.Combine(dir, Path.GetFileNameWithoutExtension(inputTarget) + ".log.xml");

                // === Build hierarchy structure ===
                var rootNode = new XElement(
                    result.HierarchyChain.First().AvevaType,
                    new XAttribute("AvevaTag", result.HierarchyChain.First().AvevaTag ?? string.Empty),
                    new XAttribute("IsVirtual", !result.IsConsistent ? "True" : "False"),
                    new XAttribute("Messages", string.Join(" | ", result.Message ?? new List<string>())),
                    new XAttribute("Warnings", string.Join(" | ", result.Warning ?? new List<string>())),
                    new XAttribute("Errors", string.Join(" | ", result.Error ?? new List<string>()))
                );

                XElement current = rootNode;
                foreach (var node in result.HierarchyChain.Skip(1))
                {
                    var elem = new XElement(node.AvevaType,
                        new XAttribute("AvevaTag", node.AvevaTag ?? string.Empty),
                        new XAttribute("Parent", node.ParentAvevaTag ?? string.Empty));
                    current.Add(elem);
                    current = elem;
                }

                var structureElement = new XElement("Hierarchy", rootNode);

                // === Safe append logic ===
                XDocument doc;
                if (File.Exists(file) && new FileInfo(file).Length > 0)
                {
                    try
                    {
                        doc = XDocument.Load(file);
                        if (doc.Root == null)
                            throw new XmlException("Root element missing");
                    }
                    catch (Exception)
                    {
                        // Corrupted or empty file – recreate it
                        doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("Hierarchies"));
                    }
                }
                else
                {
                    // Create a new document
                    doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("Hierarchies"));
                }

                // Append and save
                doc.Root!.Add(structureElement);
                doc.Save(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to append hierarchy XML: {ex.Message}");
            }
        }



        /// <summary>
        /// Writes accumulated hierarchy warnings/errors to a .log.xml beside the output file.
        /// </summary>
        public void AppendLog(string outputPath)
        {
            try
            {
                var dir = Path.GetDirectoryName(outputPath)!;
                var logPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(outputPath) + ".log.xml");

                var doc = new XDocument(
                    new XElement("Logs",
                        _localLog.Select(l => new XElement("Entry",
                            new XAttribute("Timestamp", DateTime.Now.ToString("u")),
                            new XAttribute("Message", l)))));

                if (File.Exists(logPath))
                {
                    var existing = XDocument.Load(logPath);
                    existing.Root?.Add(doc.Root?.Elements() ?? []);
                    existing.Save(logPath);
                }
                else
                {
                    doc.Save(logPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to append hierarchy log: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper for XML writer — adds a hierarchy reference link.
        /// </summary>
        internal void WriteHierarchyLink(XmlWriter writer, string link)
        {
            writer.WriteAttributeString("SourceLink", link);
        }
    }
}