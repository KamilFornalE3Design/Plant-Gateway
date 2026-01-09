using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.Extensions;
using System.Text.RegularExpressions;

namespace SMSgroup.Aveva.Utilities.Engines
{
    /// <summary>
    /// Smart tokenizer that recognizes affix, base, and suffix tokens.
    /// Uses TokenRegexMap, TechnicalOrderStructureMap, and Discipline/Entity maps.
    /// </summary>
    public sealed class TokenEngine : IEngine
    {
        #region === 🧠 Constructor & Initialization ===

        // === Dependent services ===
        private readonly IDisciplineHierarchyTokenMapService _disciplineHierarchyTokenMapService;
        private readonly ITokenRegexMapService _tokenRegexMapService;
        private readonly IDisciplineMapService _disciplineMapService;
        private readonly IEntityMapService _entityMapService;
        private readonly ICodificationMapService _codificationMapService;

        // === Cached maps ===
        private IDictionary<string, TokenRegexDTO> _tokenRegexMap = default!;
        private DisciplineHierarchyTokenMapDTO _disciplineHierarchyTokenMap = default!;
        private DisciplineMapDTO _disciplineMap = default!;
        private EntityMapDTO _entityMap = default!;
        private CodificationMapDTO _codificationMap = default!;

        // === Cached lookup ===
        private HashSet<string> _excludedKeys = new();

        // === Lock for thread safety ===
        private readonly object _syncRoot = new object();

        public TokenEngine(IDisciplineHierarchyTokenMapService disciplineHierarchyTokenMapService, ITokenRegexMapService tokenRegexMapService, IDisciplineMapService disciplineMapService, IEntityMapService entityMapService, ICodificationMapService codificationMapService)
        {
            _disciplineHierarchyTokenMapService = disciplineHierarchyTokenMapService ?? throw new ArgumentNullException(nameof(disciplineHierarchyTokenMapService));
            _tokenRegexMapService = tokenRegexMapService ?? throw new ArgumentNullException(nameof(tokenRegexMapService));
            _disciplineMapService = disciplineMapService ?? throw new ArgumentNullException(nameof(disciplineMapService));
            _entityMapService = entityMapService ?? throw new ArgumentNullException(nameof(entityMapService));
            _codificationMapService = codificationMapService ?? throw new ArgumentNullException(nameof(codificationMapService));

            // Initialize immediately upon creation
            Init();
        }


        /// <summary>
        /// Initializes or reloads all dependent maps from their respective services.
        /// This method can be called safely multiple times during CLI runtime
        /// to refresh the engine's internal data after configuration changes.
        /// </summary>
        public void Init()
        {
            lock (_syncRoot)
            {
                // === 1️ Load all maps from their services ===
                _tokenRegexMap = _tokenRegexMapService.GetMap().TokenRegex;
                _disciplineHierarchyTokenMap = _disciplineHierarchyTokenMapService.GetMap();
                _disciplineMap = _disciplineMapService.GetMap();
                _entityMap = _entityMapService.GetMap();
                _codificationMap = _codificationMapService.GetMap();

                // === 2️ Build cached exclusion keys ===
                _excludedKeys = BuildExclusionSet();
            }
        }

        /// <summary>
        /// Forces reloading of all map files from disk through their services.
        /// Useful when configuration JSON files are updated during runtime.
        /// </summary>
        public void ReloadMaps()
        {
            lock (_syncRoot)
            {
                // Ask each service to reload its map
                _tokenRegexMapService.Reload();
                _disciplineHierarchyTokenMapService.Reload();
                _disciplineMapService.Reload();
                _entityMapService.Reload();
                _codificationMapService.Reload();

                // Re-run initialization
                Init();
            }
        }

        #endregion


        #region === 🚀 Public API ===


        public TokenEngineResult Process(TakeOverPointDTO dto) => new TokenEngineResult();

        public TokenEngineResult Execute(ProjectStructureDTO dto)
        {
            var result = new TokenEngineResult();

            PreProcess(dto, result);
            Process(dto, result);
            PostProcess(dto, result);

            return result;
        }

        #endregion


        #region === Token Processing Steps ===

        private void PreProcess(ProjectStructureDTO dto, TokenEngineResult result)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(result);

            // === 1️ Validate input ===
            if (string.IsNullOrWhiteSpace(dto.AvevaTag))
            {
                result.AddError("❌ Empty AvevaTag – tokenization aborted.");
                result.IsValid = false;
                return;
            }

            // === 2️ Ensure maps are ready ===
            EnsureInitialized();

            // === 3️ Normalize and split input ===
            var input = dto.AvevaTag.Trim().ToUpperInvariant();
            var normalized = input.Replace('-', '_').Replace('.', '_');
            var parts = normalized.Split('_', StringSplitOptions.RemoveEmptyEntries);

            // === 4️ Initialize result metadata ===
            result.SourceDtoId = dto.Id;
            result.RawInputValue = input;
            result.NormalizedInputValue = normalized;
            result.Message.Clear();
            result.Warning.Clear();
            result.Error.Clear();
            result.Tokens.Clear();
            result.ExcludedTokens.Clear();

            // === 5️ Attach transient data for next phase ===
            result.AddMessage($"🧩 PreProcess: normalized input '{normalized}' into {parts.Length} segments.");
        }
        /// <summary>
        /// Core token recognition pipeline.
        /// Responsible for detecting affix, base, suffix, and dynamic tokens,
        /// and writing them into <see cref="TokenEngineResult.Tokens"/>.
        /// </summary>
        private void Process(ProjectStructureDTO dto, TokenEngineResult result)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(result);

            EnsureInitialized();

            // STEP 1: PREPARE LOCAL STATE
            result.Tokens.Clear();
            result.AddMessage($"🔍 Tokenization started for '{result.RawInputValue}'.");

            var normalized = result.RawInputValue.Replace('-', '_').Replace('.', '_');
            var parts = normalized.Split('_', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                result.AddError("❌ No valid segments found for tokenization.");
                result.IsValid = false;
                return;
            }

            // Create a temporary working set
            var recognizedTokens = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);

            // STEP 2: DETECT AFFIX TOKENS
            try
            {
                ProcessAffix(parts, recognizedTokens);
            }
            catch (Exception ex)
            {
                result.AddWarning($"⚠️ Affix detection error: {ex.Message}");
            }

            // STEP 3: DETECT BASE TOKENS
            try
            {
                ProcessBase(parts, recognizedTokens);
            }
            catch (Exception ex)
            {
                result.AddWarning($"⚠️ Base detection error: {ex.Message}");
            }

            // STEP 4: DETECT SUFFIX TOKENS
            try
            {
                ProcessSuffix(parts, recognizedTokens);
            }
            catch (Exception ex)
            {
                result.AddWarning($"⚠️ Suffix detection error: {ex.Message}");
            }

            // STEP 5: DETECT EXCEPTION OR DYNAMIC TOKENS
            try
            {
                DetectExceptionPatterns(parts, result);
            }
            catch (Exception ex)
            {
                result.AddWarning($"⚠️ Exception pattern detection error: {ex.Message}");
            }

            // STEP 6: NORMALIZE REPLACEMENTS
            try
            {
                NormalizeReplacementPositions(recognizedTokens, _tokenRegexMapService);
            }
            catch (Exception ex)
            {
                result.AddWarning($"⚠️ Replacement normalization error: {ex.Message}");
            }

            // STEP 7: MERGE & FINALIZE
            foreach (var (key, token) in recognizedTokens)
            {
                if (!result.Tokens.ContainsKey(key))
                    result.Tokens[key] = token;
            }

            result.AddMessage($"✅ Processed {result.Tokens.Count} tokens.");
            result.IsValid = result.Tokens.Values.All(t => !t.IsMissing);

            if (!result.IsValid)
                result.AddWarning($"⚠️ Some tokens are missing or invalid in '{result.RawInputValue}'.");
        }
        /// <summary>
        /// Finalizes the tokenization result:
        /// - Orders tokens by position
        /// - Separates excluded/invalid tokens
        /// - Performs consistency validation
        /// - Computes success flags and summary messages
        /// </summary>
        private void PostProcess(ProjectStructureDTO dto, TokenEngineResult result)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(result);

            // 🧩 STEP 1: SAFETY CHECK
            if (result.Tokens == null || result.Tokens.Count == 0)
            {
                result.AddWarning("⚠️ No tokens found to post-process.");
                result.IsValid = false;
                return;
            }

            // 🧱 STEP 2: SEPARATE EXCLUDED TOKENS
            // Move MISSING or useless tokens into ExcludedTokens
            Dictionary<string, Token> valid;
            Dictionary<string, Token> excluded;

            try
            {
                (valid, excluded) = SeparateExcludedTokens(result.Tokens);
            }
            catch (Exception ex)
            {
                result.AddWarning($"⚠️ Error while separating excluded tokens: {ex.Message}");
                valid = result.Tokens;
                excluded = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);
            }

            result.ExcludedTokens = excluded;

            // STEP 3: ORDER VALID TOKENS
            try
            {
                result.Tokens = OrderTokensByPosition(valid);
            }
            catch (Exception ex)
            {
                result.AddWarning($"⚠️ Error while ordering tokens: {ex.Message}");
            }

            // STEP 4: RUN CONSISTENCY CHECK
            try
            {
                TokenConsistencyCheck(result);
            }
            catch (Exception ex)
            {
                result.AddWarning($"⚠️ Consistency check failed: {ex.Message}");
            }

            // STEP 5: COMPUTE FLAGS
            result.IsValid = result.Tokens.Values.All(t => !t.IsMissing && !string.IsNullOrWhiteSpace(t.Value));
            result.IsConsistencyChecked = true;

            // STEP 6: SUMMARIZE
            if (result.IsSuccess)
            {
                result.AddMessage($"✅ PostProcess complete — {result.Tokens.Count} tokens ready.");
            }
            else if (!result.IsValid)
            {
                result.AddWarning("⚠️ PostProcess detected invalid or missing tokens.");
            }
            else
            {
                result.AddWarning("⚠️ PostProcess completed with partial success.");
            }

            // Log excluded tokens count for diagnostics
            if (result.ExcludedTokens.Any())
                result.AddMessage($"🧹 {result.ExcludedTokens.Count} token(s) moved to ExcludedTokens.");
        }

        #endregion


        #region === Token Processing Phases ===

        private void ProcessAffix(string[] parts, IDictionary<string, Token> resultTokens)
        {
            if (parts is null || parts.Length == 0)
                return;

            // === 1️⃣ Get all affix-type definitions from TokenRegexMap ===
            var affixDefinitions = _tokenRegexMap
                .Where(kv => kv.Value.Type.Equals("affix", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (affixDefinitions.Count == 0)
                return;

            // === 2️⃣ Iterate parts and check each against affix regex ===
            foreach (var kv in affixDefinitions)
            {
                var regexDef = kv.Value;
                var key = kv.Key;

                if (string.IsNullOrWhiteSpace(regexDef.Pattern))
                    continue;

                foreach (var part in parts)
                {
                    if (Regex.IsMatch(part, $"^{regexDef.Pattern}$", RegexOptions.IgnoreCase))
                    {
                        // === 3️⃣ Create and store affix token ===
                        var token = new Token
                        {
                            Key = key,
                            Value = part,
                            Position = regexDef.Position,
                            Type = regexDef.Type,
                            Pattern = regexDef.Pattern,
                            IsMatch = true,
                            SourceMapKey = key,
                            Note = $"Detected affix token '{part}'"
                        };

                        resultTokens[key] = token;

                        // Only one affix expected → stop after first match
                        return;
                    }
                }
            }
        }

        private void ProcessBase(string[] parts, IDictionary<string, Token> resultTokens)
        {
            if (parts is null || parts.Length == 0)
                return;

            // === 1️⃣ Group base definitions by position ===
            var baseTokensByPos = _tokenRegexMap
                .Where(kv => kv.Value.Type.Equals("base", StringComparison.OrdinalIgnoreCase))
                .GroupBy(kv => kv.Value.Position)
                .ToDictionary(g => g.Key, g => g.ToList());

            // === 2️⃣ Get maximum position for base tokens (avoid suffix range) ===
            var maxBasePos = _tokenRegexMapService.GetHighestBasePosition();

            // === 3️⃣ Evaluate input parts ===
            for (int index = 0; index < parts.Length; index++)
            {
                var value = parts[index];
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                // Skip suffix or exception parts
                if (index > maxBasePos)
                    break;

                // Get expected base token for this position
                var baseDef = _tokenRegexMapService.GetTokenForPosition(index);
                if (baseDef == null || string.IsNullOrWhiteSpace(baseDef.Name))
                    continue;

                // Check if we have matching regex definitions for this position
                if (!baseTokensByPos.TryGetValue(index, out var candidates))
                {
                    resultTokens[baseDef.Name] = new Token
                    {
                        Key = baseDef.Name,
                        Value = $"MISSING_{baseDef.Name.ToUpperInvariant()}",
                        Type = "base",
                        Position = index,
                        IsMissing = true,
                        SourceMapKey = baseDef.Name,
                        Note = $"No regex candidates found for {baseDef.Name} at position {index}"
                    };
                    continue;
                }

                // Try matching the primary rule first
                var primaryRule = candidates.FirstOrDefault(t =>
                    t.Key.Equals(baseDef.Name, StringComparison.OrdinalIgnoreCase));

                if (!primaryRule.Equals(default(KeyValuePair<string, TokenRegexDTO>)) &&
                    Regex.IsMatch(value, $"^{primaryRule.Value.Pattern}$", RegexOptions.IgnoreCase))
                {
                    resultTokens[baseDef.Name] = new Token
                    {
                        Key = baseDef.Name,
                        Value = value,
                        Type = "base",
                        Position = index,
                        Pattern = primaryRule.Value.Pattern,
                        IsMatch = true,
                        SourceMapKey = primaryRule.Key,
                        Note = $"Matched base token '{value}' at position {index}"
                    };
                    continue;
                }

                // Try fallback (alternative) candidates
                var fallback = candidates.FirstOrDefault(t =>
                    !t.Key.Equals(baseDef.Name, StringComparison.OrdinalIgnoreCase) &&
                    Regex.IsMatch(value, $"^{t.Value.Pattern}$", RegexOptions.IgnoreCase));

                if (!fallback.Equals(default(KeyValuePair<string, TokenRegexDTO>)))
                {
                    resultTokens[fallback.Key] = new Token
                    {
                        Key = fallback.Key,
                        Value = value,
                        Type = "base",
                        Position = index,
                        Pattern = fallback.Value.Pattern,
                        IsMatch = true,
                        SourceMapKey = fallback.Key,
                        IsReplacement = true,
                        ReplacesKey = baseDef.Name,
                        Note = $"Matched fallback token '{value}' replacing base '{baseDef.Name}'"
                    };
                    continue;
                }

                // If nothing matched, mark missing
                resultTokens[baseDef.Name] = new Token
                {
                    Key = baseDef.Name,
                    Value = $"MISSING_{baseDef.Name.ToUpperInvariant()}",
                    Type = "base",
                    Position = index,
                    IsMissing = true,
                    SourceMapKey = baseDef.Name,
                    Note = $"No match found for base token '{baseDef.Name}' at position {index}"
                };
            }
        }

        /// <summary>
        /// Processes all suffix-level tokens (Discipline, Entity, TagComposite, PlantLayout*, etc.)
        /// based purely on TokenRegexMap and map-defined suffix types.
        /// Automatically detects Discipline/Entity from their maps.
        /// </summary>
        private void ProcessSuffix(string[] parts, IDictionary<string, Token> resultTokens)
        {
            EnsureInitialized();

            // 1) Load discipline/entity codes
            var disciplineCodes = _disciplineMap.Disciplines
                .SelectMany(d => new[] { d.Key, d.Value.Code })
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var entityCodes = _entityMap.Entities
                .SelectMany(e => new[] { e.Key, e.Value.Code })
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 2) All suffix definitions
            var suffixDefs = _tokenRegexMap
                .Where(kv => kv.Value.Type.Equals("suffix", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

            // 3) Base definitions by position (pos -> base key)
            var baseByPos = _tokenRegexMap
                .Where(kv => kv.Value.Type.Equals("base", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kv => kv.Value.Position, kv => kv.Key);

            // Helpers
            string? FindBaseToReplace(int suffixPos) =>
                baseByPos.Where(kv => kv.Key <= suffixPos)
                         .OrderByDescending(kv => kv.Key)
                         .Select(kv => kv.Value)
                         .FirstOrDefault();

            string? FindNextBase(int suffixPos) =>
                baseByPos.TryGetValue(suffixPos + 1, out var name) ? name : null;

            int GetBasePos(string baseKey) =>
                _tokenRegexMap.TryGetValue(baseKey, out var dto) ? dto.Position : -1;

            // Prevent suffixes from consuming already recognized base values
            var currentBaseValues = resultTokens.Values
                .Where(t => string.Equals(t.Type, "base", StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 4) Evaluate suffix tokens
            foreach (var (suffixKey, def) in suffixDefs)
            {
                if (string.IsNullOrWhiteSpace(def.Pattern))
                    continue;

                // Discipline / Entity handled separately
                if (suffixKey.Equals("Discipline", StringComparison.OrdinalIgnoreCase))
                {
                    TryAddDisciplineToken(resultTokens, disciplineCodes, def, parts);
                    continue;
                }
                if (suffixKey.Equals("Entity", StringComparison.OrdinalIgnoreCase))
                {
                    TryAddEntityToken(resultTokens, entityCodes, def, parts);
                    continue;
                }

                var rx = new Regex($"^{def.Pattern}$", RegexOptions.IgnoreCase);

                // Scan input parts only; ignore base values we already recognized
                foreach (var p in parts)
                {
                    if (string.IsNullOrWhiteSpace(p) || !rx.IsMatch(p))
                        continue;
                    if (currentBaseValues.Contains(p))
                        continue;

                    // Which base (by position) does this suffix replace?
                    var replacesBase = FindBaseToReplace(def.Position);
                    if (string.IsNullOrWhiteSpace(replacesBase))
                        continue;

                    // Replace only if base is missing/invalid
                    bool baseValid = resultTokens.TryGetValue(replacesBase, out var baseTok)
                                     && baseTok != null
                                     && !baseTok.IsMissing
                                     && !string.IsNullOrWhiteSpace(baseTok.Value)
                                     && !baseTok.Value.StartsWith("MISSING_", StringComparison.OrdinalIgnoreCase);
                    if (baseValid)
                        continue;

                    // Default target is the same base slot we replace
                    var targetKey = replacesBase;

                    // Special: TagIncremental (pos 3) lands on next base (e.g., Component at pos 4) if present
                    if (suffixKey.Equals("TagIncremental", StringComparison.OrdinalIgnoreCase))
                    {
                        var nextBase = FindNextBase(def.Position);
                        if (!string.IsNullOrWhiteSpace(nextBase))
                            targetKey = nextBase;
                    }

                    // Set final position to the TARGET BASE position (ordering correctness)
                    var targetPos = GetBasePos(targetKey);

                    var token = new Token
                    {
                        Key = targetKey,                 // stored under base key (or next base for TagIncremental)
                        Value = p,
                        Type = def.Type,
                        Pattern = def.Pattern,
                        Position = targetPos,
                        IsMatch = true,
                        IsReplacement = true,
                        ReplacesKey = replacesBase,      // base conceptually replaced
                        ReplacedBy = suffixKey,          // which suffix performed the replacement
                        SourceMapKey = suffixKey,
                        Note = $"Suffix '{suffixKey}' matched '{p}' → {targetKey} (replaces {replacesBase})"
                    };

                    // Write/overwrite under the BASE key
                    resultTokens[targetKey] = token;

                    // Ensure no raw suffix entry remains
                    if (resultTokens.ContainsKey(suffixKey))
                        resultTokens.Remove(suffixKey);
                }
            }

            // 5) TagComposite merge only if TagComposite exists (we don't synthesize it)
            if (suffixDefs.TryGetValue("TagComposite", out var compDef) && resultTokens.ContainsKey("TagComposite"))
            {
                var compositeParts = resultTokens.Values
                    .Where(t => t.Key.Equals("TagComposite", StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (compositeParts.Count > 1)
                {
                    resultTokens["TagComposite"] = new Token
                    {
                        Key = "TagComposite",
                        Value = string.Join("-", compositeParts),
                        Type = compDef.Type,
                        Pattern = compDef.Pattern,
                        Position = compDef.Position,
                        IsMatch = true,
                        SourceMapKey = "TagComposite",
                        Note = $"Merged {compositeParts.Count} TagComposite segments"
                    };
                }
            }
        }

        #endregion

        #region === Private Helpers ===

        private void ApplySuffixAsBaseReplacement(
    string suffixKey, string matchedValue, TokenRegexDTO def,
    IDictionary<string, Token> resultTokens)
        {
            // Map: position -> base key
            var baseByPos = _tokenRegexMap
                .Where(kv => kv.Value.Type.Equals("base", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kv => kv.Value.Position, kv => kv.Key);

            // find base at/left of suffix position
            var replacesBase = baseByPos
                .Where(kv => kv.Key <= def.Position)
                .OrderByDescending(kv => kv.Key)
                .Select(kv => kv.Value)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(replacesBase))
                return;

            // only replace if base is missing or MISSING_*
            bool baseValid = resultTokens.TryGetValue(replacesBase, out var baseTok)
                             && baseTok != null
                             && !baseTok.IsMissing
                             && !string.IsNullOrWhiteSpace(baseTok.Value)
                             && !baseTok.Value.StartsWith("MISSING_", StringComparison.OrdinalIgnoreCase);
            if (baseValid)
                return;

            // default target is the base we replace
            var targetKey = replacesBase;

            // special: TagIncremental "lands" on next base (Component if defined)
            if (suffixKey.Equals("TagIncremental", StringComparison.OrdinalIgnoreCase))
            {
                var nextBase = baseByPos.TryGetValue(def.Position + 1, out var nb) ? nb : null;
                if (!string.IsNullOrWhiteSpace(nextBase))
                    targetKey = nextBase;
            }

            var token = new Token
            {
                Key = targetKey,
                Value = matchedValue,
                Type = def.Type,
                Pattern = def.Pattern,
                Position = def.Position,
                IsMatch = true,
                IsReplacement = true,
                ReplacesKey = replacesBase,
                ReplacedBy = suffixKey,
                SourceMapKey = suffixKey,
                Note = $"Suffix '{suffixKey}' matched '{matchedValue}' → {targetKey} (replaces {replacesBase})"
            };

            resultTokens[targetKey] = token;

            // ensure no raw suffix entry remains
            if (resultTokens.ContainsKey(suffixKey))
                resultTokens.Remove(suffixKey);
        }


        /// <summary>
        /// Moves invalid, missing, or replaced tokens into ExcludedTokens.
        /// Returns a cleaned, valid-only token dictionary.
        /// </summary>
        private static (Dictionary<string, Token> valid, Dictionary<string, Token> excluded) SeparateExcludedTokens(Dictionary<string, Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return (tokens, new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase));

            var valid = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);
            var excluded = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in tokens)
            {
                var token = kv.Value;

                // === Exclusion criteria ===
                bool isUseless =
                    token == null ||
                    token.IsMissing ||
                    (token.IsReplacement && !token.IsMatch) ||
                    string.IsNullOrWhiteSpace(token.Value) ||
                    token.Value.StartsWith("MISSING_", StringComparison.OrdinalIgnoreCase);

                if (isUseless)
                {
                    excluded[kv.Key] = token;
                    continue;
                }

                valid[kv.Key] = token;
            }

            return (valid, excluded);
        }

        /// <summary>
        /// Orders the token dictionary by Position (ascending), keeping suffixes without
        /// positions at the end. Rebuilds dictionary in correct order for serialization.
        /// </summary>
        private static Dictionary<string, Token> OrderTokensByPosition(Dictionary<string, Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return tokens;

            return tokens
                .OrderBy(kv => kv.Value.Position >= 0 ? kv.Value.Position : int.MaxValue)
                .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Detects and adds the Discipline token if found at the end of the input or in tokens.
        /// </summary>
        private static void TryAddDisciplineToken(IDictionary<string, Token> resultTokens, HashSet<string> disciplineCodes, TokenRegexDTO def, string[] parts)
        {
            if (resultTokens.ContainsKey("Discipline"))
                return;

            // 1️ Search suffix parts (reverse order)
            foreach (var part in parts.Reverse())
            {
                if (disciplineCodes.Contains(part))
                {
                    resultTokens["Discipline"] = new Token
                    {
                        Key = "Discipline",
                        Value = part,
                        Type = def.Type,
                        Pattern = def.Pattern,
                        IsMatch = true,
                        SourceMapKey = "Discipline",
                        Position = int.MaxValue, // ensures it sorts last
                        Note = $"Matched discipline code '{part}'"
                    };
                    return;
                }
            }
        }

        /// <summary>
        /// Detects and adds the Entity token if found at the end of the input or in tokens.
        /// </summary>
        private static void TryAddEntityToken(IDictionary<string, Token> resultTokens, HashSet<string> entityCodes, TokenRegexDTO def, string[] parts)
        {
            if (resultTokens.ContainsKey("Entity"))
                return;

            // 1️ Search suffix parts (reverse order)
            foreach (var part in parts.Reverse())
            {
                if (entityCodes.Contains(part))
                {
                    resultTokens["Entity"] = new Token
                    {
                        Key = "Entity",
                        Value = part,
                        Type = def.Type,
                        Pattern = def.Pattern,
                        IsMatch = true,
                        SourceMapKey = "Entity",
                        Position = int.MaxValue, // ensures it sorts last
                        Note = $"Matched entity code '{part}'"
                    };
                    return;
                }
            }
        }

        #endregion



        public void DetectExceptionPatterns(string[] parts, TokenEngineResult result)
        {
            EnsureInitialized();
            if (parts == null || parts.Length == 0 || result == null) return;

            // Build candidate suffixes (exclude ones you already treat normally)
            var candidates = _tokenRegexMap
                .Where(kv =>
                    kv.Value != null &&
                    !string.IsNullOrWhiteSpace(kv.Value.Pattern) &&
                    kv.Value.Type.Equals("suffix", StringComparison.OrdinalIgnoreCase) &&
                    !_excludedKeys.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

            // Avoid matching base values (e.g., ENS01)
            var currentBaseValues = result.Tokens.Values
                .Where(t => string.Equals(t.Type, "base", StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var rawPart in parts)
            {
                var part = rawPart?.Trim();
                if (string.IsNullOrWhiteSpace(part)) continue;
                if (currentBaseValues.Contains(part)) continue;

                // Skip Discipline/Entity here — they’re handled elsewhere
                foreach (var kv in candidates)
                {
                    var suffixKey = kv.Key;
                    if (suffixKey.Equals("Discipline", StringComparison.OrdinalIgnoreCase) ||
                        suffixKey.Equals("Entity", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var pattern = kv.Value.Pattern;
                    if (string.IsNullOrWhiteSpace(pattern)) continue;

                    if (Regex.IsMatch(part, "^" + pattern + "$", RegexOptions.IgnoreCase))
                    {
                        // Route through the SAME replacement rule as ProcessSuffix
                        ApplySuffixAsBaseReplacement(suffixKey, part, kv.Value, result.Tokens);
                        result.AddMessage($"Detected {suffixKey} token '{part}'");
                        break;
                    }
                }
            }
        }


        private HashSet<string> BuildExclusionSet()
        {
            var exclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // === 1️⃣ Add codification keys (Plant / Unit / Section / Equipment) ===
            foreach (var key in _codificationMap.Codifications.Keys)
                exclusions.Add(key);

            // === 2️⃣ Add discipline codes (ME, EL, CI, etc.) ===
            foreach (var key in _disciplineMap.Disciplines.Keys)
                exclusions.Add(key);

            // === 3️⃣ Add entity types (SDE, NOZ, DATUM, etc.) ===
            foreach (var key in _entityMap.Entities.Keys)
                exclusions.Add(key);

            // === 4️⃣ Add special/internal TokenRegex definitions to ignore ===
            foreach (var kv in _tokenRegexMap)
            {
                var key = kv.Key;
                var dto = kv.Value;

                // Skip null or invalid patterns
                if (dto == null || string.IsNullOrWhiteSpace(key))
                    continue;

                // Skip core tokens handled by Codification
                if (key.Equals("Plant", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("PlantUnit", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("PlantSection", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Equipment", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Exclude special tokens and meta definitions
                if (key.StartsWith("TagBase", StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith("TagComposite", StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith("Project", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("Placeholder", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("System", StringComparison.OrdinalIgnoreCase))
                {
                    exclusions.Add(key);
                }
            }

            return exclusions;
        }

        /// <summary>
        /// Validates token order consistency against defined codification hierarchy.
        /// Does not modify token order; only sets flags and adds warnings if the
        /// observed sequence differs from expected TokenRegexMap positions.
        /// </summary>
        public void TokenConsistencyCheck(TokenEngineResult result)
        {
            if (result == null || result.Tokens == null || result.Tokens.Count == 0)
                return;

            result.IsConsistencyChecked = true;

            // === 1️⃣ Build ordered list of known positions from TokenRegexMap ===
            var expectedOrder = _tokenRegexMap
                .Where(kv => kv.Value != null && kv.Value.Position >= 0)
                .OrderBy(kv => kv.Value.Position)
                .Select(kv => kv.Key)
                .ToList();

            // === 2️⃣ Extract current token sequence ===
            var currentOrder = result.Tokens.Keys.ToList();

            // === 3️⃣ Compare expected sequence to actual (only those that exist) ===
            var filteredExpected = expectedOrder
                .Where(expected => currentOrder.Contains(expected, StringComparer.OrdinalIgnoreCase))
                .ToList();

            bool isConsistent = true;
            int lastExpectedIndex = -1;

            foreach (var token in currentOrder)
            {
                int expectedIndex = filteredExpected.IndexOf(token);
                if (expectedIndex == -1)
                    continue; // token not part of codification ordering (e.g., exception type)

                if (expectedIndex < lastExpectedIndex)
                {
                    isConsistent = false;
                    result.AddWarning($"⚠️ Token order inconsistency detected near '{token}'.");
                    break;
                }

                lastExpectedIndex = expectedIndex;
            }

            if (isConsistent)
            {
                result.AddMessage("✅ Token order consistency verified.");
            }
            else
            {
                result.AddWarning("⚠️ Token hierarchy order does not match expected codification sequence.");
            }
        }


        // ============================================================
        // 🔧 Support
        // ============================================================
        private static TokenEngineResult Invalid(ProjectStructureDTO dto, string msg) =>
            new TokenEngineResult
            {
                SourceDtoId = dto.Id,
                IsValid = false,
                Message = new List<string>()  // must be initialized
            }.Also(r => r.AddError(msg));

        /// <summary>
        /// Throws InvalidOperationException if maps were not initialized.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_tokenRegexMap == null ||
                _disciplineHierarchyTokenMap == null ||
                _disciplineMap == null ||
                _entityMap == null ||
                _codificationMap == null)
            {
                throw new InvalidOperationException("❌ TokenEngine is not initialized. Call Init() before performing token operations.");
            }
        }

        private void NormalizeReplacementPositions(IDictionary<string, Token> tokens, ITokenRegexMapService tokenRegexMapService)
        {
            var toReAdd = new List<(string key, Token token)>();
            var toRemove = new List<string>();

            foreach (var kv in tokens.ToList())
            {
                var token = kv.Value;
                if (!token.IsReplacement || string.IsNullOrWhiteSpace(token.ReplacesKey))
                    continue;

                // Force the token's position to the TARGET BASE position (where it's stored)
                var targetBaseDef = tokenRegexMapService.GetMap().TokenRegex
                    .FirstOrDefault(x => x.Key.Equals(token.Key, StringComparison.OrdinalIgnoreCase)).Value;

                if (targetBaseDef != null && targetBaseDef.Position >= 0)
                    token.Position = targetBaseDef.Position;

                // Decide if we should remove the replaced base token entry:
                // - same-level replacement (Key == ReplacesKey) → remove the old base entry
                // - cross-level (Key != ReplacesKey) → KEEP the replaced base entry (so MISSING_* stays and goes to ExcludedTokens)
                bool sameLevel = string.Equals(token.Key, token.ReplacesKey, StringComparison.OrdinalIgnoreCase);
                if (sameLevel && tokens.ContainsKey(token.ReplacesKey))
                {
                    // If we're replacing the same slot, we can safely remove the old "missing" entry
                    toRemove.Add(token.ReplacesKey);
                }

                // Re-add (ensures order is recalculated after removals)
                toReAdd.Add((kv.Key, token));
            }

            // Apply removals
            foreach (var key in toRemove.Distinct(StringComparer.OrdinalIgnoreCase))
                tokens.Remove(key);

            // Re-insert tokens (idempotent)
            foreach (var (key, token) in toReAdd)
                tokens[key] = token;
        }
    }
}
