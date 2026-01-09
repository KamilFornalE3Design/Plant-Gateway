using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.Tokenization;
using SMSgroup.Aveva.Utilities.Helpers;
using System.Text.RegularExpressions;

namespace PlantGateway.Domain.Services.Tokenization.Stages
{
    /// <summary>
    /// Stage that recognizes suffix-level tokens:
    /// - Discipline / Entity (via maps)
    /// - TagIncremental / Mechanical / Electrical / Structural suffixes
    /// - PlantSection-level exceptions (PlantLayoutStrip/Operating/Walkways/Buildings)
    ///
    /// Design:
    /// - Never overwrites valid base tokens created by codification / base regex.
    /// - Uses TokenRegexMap "suffix" definitions.
    /// - For PlantLayout* suffixes, acts as PlantSection fallback with IsFallback flag + message.
    /// - Updates TokenScores / TotalScore and adds user-facing diagnostics.
    /// </summary>
    public sealed class SuffixRecognitionStage : ITokenizationStage
    {
        public TokenizationStageId Id => TokenizationStageId.SuffixRecognition;

        private readonly IReadOnlyDictionary<string, TokenRegexDTO> _tokenRegexMap;
        private readonly DisciplineMapDTO _disciplineMap;
        private readonly EntityMapDTO _entityMap;
        private readonly DisciplineHierarchyTokenMapDTO _disciplineHierarchyTokenMap; // reserved for future use

        public SuffixRecognitionStage(
            IReadOnlyDictionary<string, TokenRegexDTO> tokenRegexMap,
            DisciplineMapDTO disciplineMap,
            EntityMapDTO entityMap,
            DisciplineHierarchyTokenMapDTO disciplineHierarchyTokenMap)
        {
            _tokenRegexMap = tokenRegexMap ?? throw new ArgumentNullException(nameof(tokenRegexMap));
            _disciplineMap = disciplineMap ?? new DisciplineMapDTO();
            _entityMap = entityMap ?? new EntityMapDTO();
            _disciplineHierarchyTokenMap = disciplineHierarchyTokenMap ?? new DisciplineHierarchyTokenMapDTO();
        }

        public void Execute(TokenizationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Ensure we have input segments
            var parts = context.Parts;
            if (parts == null || parts.Length == 0)
            {
                var source = !string.IsNullOrWhiteSpace(context.NormalizedInput)
                    ? context.NormalizedInput
                    : context.RawInput?.Trim().ToUpperInvariant() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(source))
                {
                    context.AddWarning("⚠️ SuffixRecognition: no input available for suffix processing.");
                    return;
                }

                var normalized = SeparatorHelper.Normalize(source);
                parts = SeparatorHelper.SplitNormalized(normalized);
                context.NormalizedInput = normalized;
                context.Parts = parts;

                context.AddMessage($"ℹ️ SuffixRecognition: rebuilt {parts.Length} segments from input '{normalized}'.");
            }

            if (parts.Length == 0)
            {
                context.AddWarning("⚠️ SuffixRecognition: normalized input produced zero segments.");
                return;
            }

            // 1) Discipline / Entity code sets from maps
            var disciplineCodes = (_disciplineMap.Disciplines ?? new Dictionary<string, DisciplineDTO>())
                .SelectMany(d => new[] { d.Key, d.Value.Code })
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var entityCodes = (_entityMap.Entities ?? new Dictionary<string, EntityDTO>())
                .SelectMany(e => new[] { e.Key, e.Value.Code })
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 2) Suffix definitions from TokenRegexMap
            var suffixDefs = _tokenRegexMap
                .Where(kv =>
                    kv.Value != null &&
                    kv.Value.Type != null &&
                    kv.Value.Type.Equals("suffix", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value,
                    StringComparer.OrdinalIgnoreCase);

            if (suffixDefs.Count == 0)
            {
                context.AddMessage("ℹ️ SuffixRecognition: no suffix definitions in TokenRegexMap – skipping stage.");
                return;
            }

            // 3) Base definitions by position (Plant, PlantUnit, PlantSection, Equipment, Component)
            var baseByPos = _tokenRegexMap
                .Where(kv =>
                    kv.Value != null &&
                    kv.Value.Type != null &&
                    kv.Value.Type.Equals("base", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    kv => kv.Value.Position,
                    kv => kv.Key);

            string? FindBaseToReplace(int suffixPos) =>
                baseByPos
                    .Where(kv => kv.Key <= suffixPos)
                    .OrderByDescending(kv => kv.Key)
                    .Select(kv => kv.Value)
                    .FirstOrDefault();

            string? FindNextBase(int suffixPos) =>
                baseByPos.TryGetValue(suffixPos + 1, out var name) ? name : null;

            int GetBasePos(string baseKey) =>
                _tokenRegexMap.TryGetValue(baseKey, out var dto) ? dto.Position : -1;

            // 4) Prevent suffixes from reusing already recognized base values
            var currentBaseValues = context.Tokens.Values
                .Where(t => string.Equals(t.Type, "base", StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 5) Evaluate suffix tokens
            foreach (var (suffixKey, def) in suffixDefs)
            {
                if (string.IsNullOrWhiteSpace(def.Pattern))
                    continue;

                // Discipline handled via map (reverse search)
                if (suffixKey.Equals("Discipline", StringComparison.OrdinalIgnoreCase))
                {
                    TryAddDisciplineToken(context, disciplineCodes, def, parts);
                    continue;
                }

                // Entity handled via map (reverse search)
                if (suffixKey.Equals("Entity", StringComparison.OrdinalIgnoreCase))
                {
                    TryAddEntityToken(context, entityCodes, def, parts);
                    continue;
                }

                var rx = new Regex($"^{def.Pattern}$", RegexOptions.IgnoreCase);

                foreach (var p in parts)
                {
                    if (string.IsNullOrWhiteSpace(p) || !rx.IsMatch(p))
                        continue;

                    // Do not reuse values already used as base tokens
                    if (currentBaseValues.Contains(p))
                        continue;

                    // Which base (by position) does this suffix conceptually replace?
                    var replacesBase = FindBaseToReplace(def.Position);
                    if (string.IsNullOrWhiteSpace(replacesBase))
                        continue;

                    var isException = IsExceptionSuffix(suffixKey);

                    // PlantSection-level exceptions only apply if PlantSection is missing/invalid
                    if (isException && !CanApplyExceptionForPlantSection(context))
                        continue;

                    //  Base key we attach to in Tokens dictionary
                    var targetKey = replacesBase;

                    // Special: TagIncremental lands on "next" base (e.g. Component)
                    if (suffixKey.Equals("TagIncremental", StringComparison.OrdinalIgnoreCase))
                    {
                        var nextBase = FindNextBase(def.Position);
                        if (!string.IsNullOrWhiteSpace(nextBase))
                            targetKey = nextBase;
                    }

                    var targetPos = GetBasePos(targetKey);

                    var note = BuildSuffixNote(
                        suffixKey,
                        replacesBase,
                        targetKey,
                        p,
                        isException);

                    var token = new Token
                    {
                        Key = targetKey,                 // stored under base key (or next base for TagIncremental)
                        Value = p,
                        Type = def.Type,
                        Pattern = def.Pattern,
                        Position = targetPos,
                        IsMatch = true,
                        IsReplacement = true,
                        ReplacesKey = replacesBase,      // base conceptually replaced (PlantSection, Equipment, Component, ...)
                        ReplacedBy = suffixKey,          // which suffix performed the replacement
                        SourceMapKey = suffixKey,
                        IsFallback = isException,
                        Note = note
                    };

                    context.Tokens[targetKey] = token;
                    currentBaseValues.Add(p);

                    // Scoring + diagnostics
                    if (isException)
                    {
                        // PlantSection-level exception fallback
                        AddScore(context, targetKey, 15);
                        context.AddMessage(
                            $"ℹ️ SuffixRecognition: exception '{suffixKey}' with value '{p}' used as fallback for missing {replacesBase} → {targetKey}.");
                    }
                    else if (suffixKey.Equals("TagIncremental", StringComparison.OrdinalIgnoreCase))
                    {
                        AddScore(context, targetKey, 10);
                        context.AddMessage(
                            $"ℹ️ SuffixRecognition: TagIncremental '{p}' attached to '{targetKey}' (replaces {replacesBase}).");
                    }
                    else
                    {
                        AddScore(context, targetKey, 8);
                    }

                    // For now: one match per suffix is enough (avoids weird duplicates)
                    break;
                }
            }

            context.AddMessage(
                $"🔎 SuffixRecognition: completed suffix/exception detection. Tokens now: {context.Tokens.Count}.");
        }

        // =====================================================================
        // Discipline / Entity helpers
        // =====================================================================

        private static void TryAddDisciplineToken(
            TokenizationContext context,
            HashSet<string> disciplineCodes,
            TokenRegexDTO def,
            string[] parts)
        {
            if (context.Tokens.ContainsKey("Discipline"))
                return;

            foreach (var part in parts.Reverse())
            {
                if (!disciplineCodes.Contains(part))
                    continue;

                context.Tokens["Discipline"] = new Token
                {
                    Key = "Discipline",
                    Value = part,
                    Type = def.Type,
                    Pattern = def.Pattern,
                    IsMatch = true,
                    SourceMapKey = "Discipline",
                    Position = int.MaxValue, // sort last
                    Note = $"Matched discipline code '{part}'."
                };

                AddScore(context, "Discipline", 20);
                context.AddMessage($"ℹ️ SuffixRecognition: detected discipline '{part}'.");
                return;
            }
        }

        private static void TryAddEntityToken(
            TokenizationContext context,
            HashSet<string> entityCodes,
            TokenRegexDTO def,
            string[] parts)
        {
            if (context.Tokens.ContainsKey("Entity"))
                return;

            foreach (var part in parts.Reverse())
            {
                if (!entityCodes.Contains(part))
                    continue;

                context.Tokens["Entity"] = new Token
                {
                    Key = "Entity",
                    Value = part,
                    Type = def.Type,
                    Pattern = def.Pattern,
                    IsMatch = true,
                    SourceMapKey = "Entity",
                    Position = int.MaxValue, // sort last
                    Note = $"Matched entity code '{part}'."
                };

                AddScore(context, "Entity", 20);
                context.AddMessage($"ℹ️ SuffixRecognition: detected entity '{part}'.");
                return;
            }
        }

        // =====================================================================
        // Exception & note helpers
        // =====================================================================

        /// <summary>
        /// PlantSection-level exception suffixes:
        /// - PlantLayout* (Strip, Operating, Walkways, Buildings)
        /// - Planned: MANDUMMY, LIFTCAR (can be added later to TokenRegexMap)
        /// </summary>
        private static bool IsExceptionSuffix(string suffixKey)
        {
            if (string.IsNullOrWhiteSpace(suffixKey))
                return false;

            if (suffixKey.StartsWith("PlantLayout", StringComparison.OrdinalIgnoreCase))
                return true;

            // Future: maintain special exceptions here if you add them to TokenRegexMap
            return suffixKey.Equals("MANDUMMY", StringComparison.OrdinalIgnoreCase)
                   || suffixKey.Equals("LIFTCAR", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Only apply PlantSection-level exceptions when we do not already
        /// have a valid PlantSection token (from codification or base regex).
        /// </summary>
        private static bool CanApplyExceptionForPlantSection(TokenizationContext context)
        {
            if (context.Tokens.TryGetValue("PlantSection", out var section) &&
                section != null &&
                !section.IsMissing &&
                !string.IsNullOrWhiteSpace(section.Value) &&
                !section.Value.StartsWith("MISSING_", StringComparison.OrdinalIgnoreCase))
            {
                // PlantSection already recognized → do not override
                return false;
            }

            return true;
        }

        private static string BuildSuffixNote(
            string suffixKey,
            string replacesBase,
            string targetKey,
            string value,
            bool isException)
        {
            if (isException)
            {
                return $"Exception suffix '{suffixKey}' matched '{value}' → {targetKey} (fallback for missing {replacesBase}).";
            }

            if (suffixKey.Equals("TagIncremental", StringComparison.OrdinalIgnoreCase))
            {
                return $"Suffix 'TagIncremental' matched '{value}' → {targetKey} (incremental, replaces {replacesBase}).";
            }

            return $"Suffix '{suffixKey}' matched '{value}' → {targetKey} (replaces {replacesBase}).";
        }

        // =====================================================================
        // Scoring helper
        // =====================================================================

        private static void AddScore(TokenizationContext context, string tokenKey, int delta)
        {
            if (context == null || string.IsNullOrWhiteSpace(tokenKey))
                return;

            if (!context.TokenScores.TryGetValue(tokenKey, out var current))
                current = 0;

            current += delta;
            context.TokenScores[tokenKey] = current;
            context.TotalScore += delta;
        }
    }
}
