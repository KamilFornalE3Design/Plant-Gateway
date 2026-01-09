using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Data.IdentityCache;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.Extensions;
using SMSgroup.Aveva.Utilities.Helpers;
using System.Reflection;

namespace SMSgroup.Aveva.Utilities.Engines
{
    /// <summary>
    /// Builds the tag suffix for an element based on TagMap definitions.
    /// Uses Discipline, Entity, and TokenEngine results to resolve suffix values.
    /// </summary>
    public sealed class SuffixEngine : IEngine
    {

        private readonly IDisciplineHierarchyTokenMapService _disciplineHierarchyTokenMapService;
        private readonly TakeOverPointCacheService _cacheService;

        public SuffixEngine(IDisciplineHierarchyTokenMapService disciplineHierarchyTokenMapService, TakeOverPointCacheService cacheService)
        {
            _disciplineHierarchyTokenMapService = disciplineHierarchyTokenMapService ?? throw new ArgumentNullException(nameof(disciplineHierarchyTokenMapService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public SuffixEngineResult Process(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> pipelineContract)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var roleResult = dto.EngineResults.OfType<RoleEngineResult>().FirstOrDefault();
            if (roleResult == null)
                throw new InvalidOperationException("❌ RoleEngineResult missing before SuffixEngine.");

            // === Extract increment from description ===
            var description = dto.Description ?? string.Empty;
            var increment = ExtractIncrement(description);

            // === Resolve role code (N for NOZZ, E for ELCONN, D for DATUM, etc.) ===
            var roleCode = ResolveRoleCode(dto, roleResult);

            // === 🧩 Cache-based fallback for self-defined geometries ===
            if (string.IsNullOrWhiteSpace(increment))
            {
                try
                {
                    var cache = _cacheService.GetCache();
                    var prefix = $"{dto.AvevaTag}-{roleCode}";

                    // Find all existing cache entries with the same base tag and role
                    var existing = cache
                        .AllKeys()
                        .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    increment = (existing.Count + 1).ToString("D2");
                }
                catch (Exception ex)
                {
                    increment = "00"; // fallback safety
                    Console.WriteLine($"⚠️ [SuffixEngine] Cache fallback failed: {ex.Message}");
                }
            }

            // === Build suffix ===
            var suffix = string.Empty;
            if (!string.IsNullOrWhiteSpace(roleCode) && !string.IsNullOrWhiteSpace(increment))
                suffix = $"{roleCode}{increment}";
            else if (!string.IsNullOrWhiteSpace(roleCode))
                suffix = roleCode;

            var isValid = !string.IsNullOrWhiteSpace(suffix);

            // === Build result ===
            var result = new SuffixEngineResult
            {
                SourceDtoId = dto.Id,
                IsValid = isValid,
                Suffix = suffix,
                TokensUsed = new List<string> { "GeometryType", "CsysDescription" },
                HasDiscipline = false,
                HasEntity = true,
                HasAnyTag = isValid
            }.Also(r => r.AddMessage(isValid ? $"✅ Suffix built: {suffix}" : $"⚠️ Missing data for suffix (role='{roleCode}', increment='{increment}')"));

            return result;
        }

        public SuffixEngineResult Process(ProjectStructureDTO dto)
        {
            var (token, discipline, entity, role) = ValidatePrerequisites(dto);
            var hierarchy = BuildHierarchyDictionary(token, discipline, entity);
            var schemas = GetSuffixSchemasForRole(dto);
            var (suffix, isValid, usedTokens) = TryBuildSuffixFromSchemas(dto, hierarchy, schemas);
            var flags = DetectSuffixFlags(token, discipline, entity);
            return BuildResult(dto, suffix, usedTokens, isValid, flags, role.AvevaType);
        }

        // ============================================================
        // 🔧 Support Methods
        // ============================================================

        private static string ComposeSuffix(List<string> tokens, List<string> values)
        {
            if (tokens == null || values == null || tokens.Count == 0 || values.Count == 0)
                return string.Empty;

            // Case: Discipline + Entity → join with underscore
            if (tokens.SequenceEqual(new[] { "Discipline", "Entity" }, StringComparer.OrdinalIgnoreCase))
                return string.Join("_", values);

            // Default: concatenate directly without separators
            return string.Concat(values);
        }
        // ============================================================
        // 🔧 Private Support Methods for SuffixEngine
        // ============================================================

        /// <summary>
        /// Ensures that all prerequisite engine results are present and valid.
        /// Throws if required components are missing.
        /// </summary>
        private (TokenEngineResult Token, DisciplineEngineResult? Discipline, EntityEngineResult? Entity, RoleEngineResult Role) ValidatePrerequisites(ProjectStructureDTO dto)
        {
            var token = dto.EngineResults.OfType<TokenEngineResult>().FirstOrDefault()
                ?? throw new InvalidOperationException("❌ TokenEngineResult missing before SuffixEngine.");

            var role = dto.EngineResults.OfType<RoleEngineResult>().FirstOrDefault()
                ?? throw new InvalidOperationException("❌ RoleEngineResult missing before SuffixEngine.");

            var discipline = dto.EngineResults.OfType<DisciplineEngineResult>().FirstOrDefault();
            var entity = dto.EngineResults.OfType<EntityEngineResult>().FirstOrDefault();

            return (token, discipline, entity, role);
        }

        /// <summary>
        /// Builds a unified hierarchy dictionary combining tokens from
        /// TokenEngine, DisciplineEngine, and EntityEngine results.
        /// </summary>
        private Dictionary<string, string> BuildHierarchyDictionary(
            TokenEngineResult tokenResult,
            DisciplineEngineResult? disciplineResult,
            EntityEngineResult? entityResult)
        {
            var hierarchy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in tokenResult.Tokens)
                hierarchy[kv.Key] = kv.Value.Value;

            if (disciplineResult != null && !string.IsNullOrWhiteSpace(disciplineResult.Discipline))
                hierarchy["Discipline"] = disciplineResult.Discipline;

            if (entityResult != null && !string.IsNullOrWhiteSpace(entityResult.Entity))
                hierarchy["Entity"] = entityResult.Entity;

            return hierarchy;
        }

        /// <summary>
        /// Resolves which token group schemas should be used for suffix generation,
        /// based on the current DTO context (discipline, role, token composition).
        /// </summary>
        private List<TokenGroupDTO> GetSuffixSchemasForRole(ProjectStructureDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // === 🔹 Extract all relevant engine results ===
            var tokenResult = dto.EngineResults.OfType<TokenEngineResult>().FirstOrDefault();
            var disciplineResult = dto.EngineResults.OfType<DisciplineEngineResult>().FirstOrDefault();
            var roleResult = dto.EngineResults.OfType<RoleEngineResult>().FirstOrDefault();

            if (tokenResult == null)
                throw new InvalidOperationException("❌ TokenEngineResult missing before SuffixEngine.");
            if (roleResult == null)
                throw new InvalidOperationException("❌ RoleEngineResult missing before SuffixEngine.");

            string discipline = disciplineResult?.Discipline?.Trim().ToUpperInvariant() ?? "DEFAULT";
            string role = roleResult.AvevaType?.Trim().ToUpperInvariant() ?? "EQUI";

            // === 🔹 Detect proper context (DEFAULT vs ST/CI) ===
            string selectedKey = DetectContext(tokenResult, discipline);

            var map = _disciplineHierarchyTokenMapService.GetMap().Disciplines;

            // === 🔹 Resolve discipline (fallback to DEFAULT if missing) ===
            if (!map.TryGetValue(selectedKey, out var disciplineDef))
                disciplineDef = map["DEFAULT"];

            // === 🔹 Try to resolve the schema for the given role ===
            if (!disciplineDef.Tokens.TryGetValue(role, out var schema))
            {
                // Fallback to DEFAULT discipline’s same role
                if (disciplineDef != map["DEFAULT"] && map["DEFAULT"].Tokens.TryGetValue(role, out var defaultSchema))
                {
                    schema = defaultSchema;
                }
                else if (map["DEFAULT"].Tokens.TryGetValue("EQUI", out var equiSchema))
                {
                    // Ultimate fallback — generic EQUI
                    schema = equiSchema;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"❌ No token schema found for role '{role}' in discipline '{discipline}', and no fallback found in DEFAULT.");
                }
            }

            // === 🔹 Base list always includes the resolved schema ===
            var result = new List<TokenGroupDTO> { schema };

            // === 🔹 Context-sensitive enrichment ===
            bool hasEquipment = tokenResult.Tokens.ContainsKey("Equipment");
            bool hasIncremental = tokenResult.Tokens.ContainsKey("TagIncremental");

            // If both Equipment + TagIncremental exist, allow composite schemas (e.g. STRU inherits EQUI suffix)
            if (hasEquipment && hasIncremental)
            {
                foreach (var kvp in disciplineDef.Tokens)
                {
                    var tg = kvp.Value;
                    bool definesIncremental =
                        tg.Base.Contains("TagIncremental", StringComparer.OrdinalIgnoreCase) ||
                        tg.Suffix.Contains("TagIncremental", StringComparer.OrdinalIgnoreCase);

                    if (definesIncremental && !result.Contains(tg))
                        result.Add(tg);
                }
            }

            return result;
        }


        private (string? Suffix, bool IsValid, List<string> UsedTokens) TryBuildSuffixFromSchemas(ProjectStructureDTO dto, Dictionary<string, string> hierarchy, IEnumerable<TokenGroupDTO> schemas)
        {
            // === STEP 1️⃣: Extract all relevant engine results ===
            var tokenEngine = dto.EngineResults.OfType<TokenEngineResult>().FirstOrDefault();
            var disciplineResult = dto.EngineResults.OfType<DisciplineEngineResult>().FirstOrDefault();
            var entityResult = dto.EngineResults.OfType<EntityEngineResult>().FirstOrDefault();

            string? disciplineValue = disciplineResult?.Discipline ?? "ME"; // guaranteed fallback
            string? entityValue = entityResult?.Entity ?? "SDE";            // guaranteed fallback

            // === STEP 2️⃣: Collect all suffix tokens expected for this DTO from schemas ===
            var expectedSuffixTokens = schemas
                .SelectMany(s => s.Suffix ?? new List<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var collectedValues = new List<string>();
            var usedTokens = new List<string>();

            // === STEP 3️⃣: Handle special-case Discipline / Entity tokens directly ===
            if (expectedSuffixTokens.Contains("Discipline", StringComparer.OrdinalIgnoreCase))
            {
                collectedValues.Add(disciplineValue);
                usedTokens.Add("Discipline");
            }

            if (expectedSuffixTokens.Contains("Entity", StringComparer.OrdinalIgnoreCase))
            {
                collectedValues.Add(entityValue);
                usedTokens.Add("Entity");
            }

            // === STEP 4️⃣: Read token presence flags dynamically from TokenEngineResult ===
            var activeTokens = new List<string>();
            if (tokenEngine != null)
            {
                var props = tokenEngine.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in props)
                {
                    if (prop.Name.StartsWith("Has") && prop.PropertyType == typeof(bool))
                    {
                        bool flagValue = (bool)(prop.GetValue(tokenEngine) ?? false);
                        if (flagValue)
                        {
                            // Extract token name from property like HasMechanicalToken → Mechanical
                            string tokenName = prop.Name
                                .Replace("Has", "")
                                .Replace("Token", "");

                            // Fix the name issue - no time for better solution now
                            if (tokenName.Equals("Incremental", StringComparison.OrdinalIgnoreCase))
                                tokenName = "TagIncremental";
                            if (tokenName.Equals("Composite", StringComparison.OrdinalIgnoreCase))
                                tokenName = "TagComposite";

                            activeTokens.Add(tokenName);
                        }
                    }
                }
            }

            // === STEP 5️⃣: Add values for other active suffix tokens found in hierarchy ===
            foreach (var token in expectedSuffixTokens)
            {
                if (token.Equals("Discipline", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("Entity", StringComparison.OrdinalIgnoreCase))
                    continue; // already handled above

                if (activeTokens.Contains(token, StringComparer.OrdinalIgnoreCase) &&
                    hierarchy.TryGetValue(token, out var val) &&
                    !string.IsNullOrWhiteSpace(val))
                {
                    // === NEW: Apply per-token separator logic ===
                    var separator = SeparatorHelper.TokenSeparators.TryGetValue(token, out var custom)
                        ? custom
                        : SeparatorHelper.Normalized;

                    // ensure value carries its prefix separator if needed
                    if (!string.IsNullOrEmpty(separator) &&
                        (separator == SeparatorHelper.Point || separator == SeparatorHelper.Dash))
                    {
                        // avoid double separator
                        if (!val.StartsWith(separator))
                            val = $"{separator}{val}";
                    }

                    collectedValues.Add(val);
                    usedTokens.Add(token);
                }
            }

            // === STEP 5.1️: Build final suffix using separator rules ===
            string? finalSuffix = null;

            if (collectedValues.Count > 0)
            {
                // Split values into prefix-type (like .001) and suffix-type (like ME, SDE)
                var prefixPart = collectedValues.FirstOrDefault(v => v.StartsWith(SeparatorHelper.Structural)) ?? string.Empty;

                var joinedCore = string.Join(
                    SeparatorHelper.Normalized,
                    collectedValues
                        .Where(v => !string.IsNullOrWhiteSpace(v) && !v.StartsWith(SeparatorHelper.Structural))
                        .Select(v => v.TrimStart('-', '.', '_'))
                );

                // Build suffix: ".001-ME_SDE" or "-ME_SDE"
                if (!string.IsNullOrEmpty(prefixPart))
                {
                    // prefix already contains its separator, ensure suffix is appended correctly
                    finalSuffix = string.Concat(
                        prefixPart,
                        string.IsNullOrEmpty(joinedCore) ? string.Empty : $"{SeparatorHelper.Suffix}{joinedCore}"
                    );
                }
                else
                {
                    finalSuffix = string.Concat(
                        SeparatorHelper.Suffix,
                        joinedCore
                    );
                }
            }

            bool isValid = !string.IsNullOrWhiteSpace(finalSuffix);

            return (finalSuffix, isValid, usedTokens);
        }

        /// <summary>
        /// Detects which suffix-related flags are active (discipline/entity/tag tokens).
        /// </summary>
        private (bool HasDiscipline, bool HasEntity, bool HasAnyTag) DetectSuffixFlags(
            TokenEngineResult tokenResult,
            DisciplineEngineResult? disciplineResult,
            EntityEngineResult? entityResult)
        {
            bool hasDiscipline =
                tokenResult.Tokens.ContainsKey("Discipline") ||
                (disciplineResult != null && !string.IsNullOrWhiteSpace(disciplineResult.Discipline));

            bool hasEntity =
                tokenResult.Tokens.ContainsKey("Entity") ||
                (entityResult != null && !string.IsNullOrWhiteSpace(entityResult.Entity));

            // Dynamically detect tag-like tokens (Tag*, Mechanical, Electrical, Structural)
            var tagTokenTypes = _disciplineHierarchyTokenMapService
                .GetMap().Disciplines.Values
                .SelectMany(v => v.Tokens.Values)
                .SelectMany(g => g.Suffix)
                .Where(t => t.StartsWith("Tag", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(t, "Mechanical", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(t, "Electrical", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(t, "Structural", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            bool hasAnyTag = tokenResult.Tokens.Keys.Any(k => tagTokenTypes.Contains(k));

            return (hasDiscipline, hasEntity, hasAnyTag);
        }

        /// <summary>
        /// Composes a clean, structured SuffixEngineResult from the computed data.
        /// </summary>
        private SuffixEngineResult BuildResult(
            ProjectStructureDTO dto,
            string? suffix,
            List<string> usedTokens,
            bool isValid,
            (bool HasDiscipline, bool HasEntity, bool HasAnyTag) flags,
            string role)
        {
            return new SuffixEngineResult
            {
                SourceDtoId = dto.Id,
                Suffix = suffix ?? string.Empty,
                TokensUsed = usedTokens,
                IsValid = isValid,
                HasDiscipline = flags.HasDiscipline,
                HasEntity = flags.HasEntity,
                HasAnyTag = flags.HasAnyTag,
                Message = new List<string>() // initialize the list
            }.Also(r => r.AddMessage(isValid ? $"✅ Tag suffix built for role '{role}' → {suffix}" : $"⚠️ Tag suffix built with missing tokens for role '{role}' → {suffix}"));
        }


        #region Private helpers
        /// <summary>
        /// Determines which discipline context ("DEFAULT", "ST", "CI") should be used
        /// for suffix schema resolution based on the active tokens in <see cref="TokenEngineResult"/>.
        /// </summary>
        private static string DetectContext(TokenEngineResult tokenResult, string discipline)
        {
            if (tokenResult == null)
                return "DEFAULT";

            // === Shortcut flags for readability ===
            bool hasPlantSection = tokenResult.HasPlantSectionToken;
            bool hasEquipment = tokenResult.HasEquipmentToken;
            bool hasIncremental = tokenResult.HasIncrementalToken;

            // === Core decision ===
            string selectedKey;

            if (hasPlantSection && hasEquipment)
            {
                // Default: Equipment-level structure (e.g., PCM01.MHS01.MFS01.STR01)
                selectedKey = "DEFAULT";
                // Example: "🏗 Context detected: Equipment-level structure → DEFAULT hierarchy."
            }
            else if (hasPlantSection && !hasEquipment && hasIncremental &&
                     (discipline.Equals("ST", StringComparison.OrdinalIgnoreCase) ||
                      discipline.Equals("CI", StringComparison.OrdinalIgnoreCase)))
            {
                // Structural or Civil section-level structure (e.g., PCM01.MHS01.MFS01.001)
                selectedKey = discipline.ToUpperInvariant();
                // Example: $"🏗 Context detected: Section-level structure for {discipline} → {discipline} hierarchy."
            }
            else
            {
                // Fallback: no matching pattern, default to generic hierarchy
                selectedKey = "DEFAULT";
                // Example: "🏗 Context fallback: DEFAULT hierarchy applied."
            }

            return selectedKey;
        }


        private static string ExtractIncrement(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            // Match patterns like HY3-BA_10_0033-16X2-B → capture "0033_1" as derived "0033.1"
            var extendedMatch = System.Text.RegularExpressions.Regex.Match(description, @"[_\-](\d{4}_\d+)(?:[_\-]|$)");
            if (extendedMatch.Success)
                return extendedMatch.Groups[1].Value;

            // Match patterns like HY3-BA_10_0033-16X2-B → capture "0033"
            var match = System.Text.RegularExpressions.Regex.Match(description, @"[_\-](\d{4})(?:[_\-]|$)");
            if (match.Success)
                return match.Groups[1].Value;

            // Fallback: try 2–3 digit numeric at end if 4-digit not found
            match = System.Text.RegularExpressions.Regex.Match(description, @"(\d{2,3})$");
            if (match.Success)
                return match.Groups[1].Value.PadLeft(4, '0');

            return string.Empty;
        }

        private static string ResolveRoleCode(TakeOverPointDTO dto, RoleEngineResult roleResult = null)
        {
            // Prefer roleResult from RoleEngine if provided
            string roleType = roleResult?.AvevaType ?? dto.GeometryType ?? string.Empty;

            if (string.IsNullOrWhiteSpace(roleType))
                return "X";

            switch (roleType.Trim().ToUpperInvariant())
            {
                case "NOZZ":
                    return "N";

                case "ELCONN":
                    return "E";

                case "DATUM":
                    return "D";

                default:
                    return "X";
            }
        }

        #endregion
    }
}
