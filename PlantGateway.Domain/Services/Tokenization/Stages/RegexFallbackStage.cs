using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.Tokenization;
using System.Text.RegularExpressions;

namespace PlantGateway.Domain.Services.Tokenization.Stages
{
    /// <summary>
    /// Stage 3: Regex-based token detection and fallback.
    /// 
    /// Responsibilities:
    /// - Use TokenRegexMap to:
    ///   * fill missing base tokens (Plant / PlantUnit / PlantSection / Equipment / Component)
    ///   * detect suffix-level tokens (Discipline, Entity, TagIncremental, TagComposite, PlantLayout*, etc.)
    /// - Apply PlantSection-level exceptions as FALLBACK (BUILDINGS / WALKWAYS / MANDUMMY / LIFTCAR / PlantLayout*).
    /// - Respect codification-first rule: NEVER overwrite a valid codification token.
    /// - Update TokenizationContext messages, warnings, and scoring.
    /// </summary>
    public sealed class RegexFallbackStage : ITokenizationStage
    {
        public TokenizationStageId Id => TokenizationStageId.RegexBaseFallback;
        public string Name => "Regex + Fallback Matching";

        private readonly ITokenRegexMapService _tokenRegexMapService;
        private readonly IDisciplineMapService _disciplineMapService;
        private readonly IEntityMapService _entityMapService;

        // Cached maps
        private Dictionary<string, TokenRegexDTO> _tokenRegexMap =
            new Dictionary<string, TokenRegexDTO>(StringComparer.OrdinalIgnoreCase);

        private DisciplineMapDTO _disciplineMap = new DisciplineMapDTO();
        private EntityMapDTO _entityMap = new EntityMapDTO();

        private bool _initialized;

        public RegexFallbackStage(
            ITokenRegexMapService tokenRegexMapService,
            IDisciplineMapService disciplineMapService,
            IEntityMapService entityMapService)
        {
            _tokenRegexMapService = tokenRegexMapService ?? throw new ArgumentNullException(nameof(tokenRegexMapService));
            _disciplineMapService = disciplineMapService ?? throw new ArgumentNullException(nameof(disciplineMapService));
            _entityMapService = entityMapService ?? throw new ArgumentNullException(nameof(entityMapService));
        }

        public void Execute(TokenizationContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            EnsureInitialized();

            var parts = context.Parts ?? Array.Empty<string>();
            if (parts.Length == 0)
            {
                context.AddWarning("⚠️ RegexFallbackStage: no segments available – skipping regex processing.");
                return;
            }

            try
            {
                DetectBaseTokens(parts, context);
                DetectSuffixTokens(parts, context);

                context.AddMessage(
                    $"🔎 RegexFallbackStage: completed regex & fallback matching. Tokens now: {context.Tokens.Count}.");
            }
            catch (Exception ex)
            {
                context.AddError($"❌ RegexFallbackStage: unexpected error: {ex.Message}");
            }
        }

        // ============================================================
        //  Init
        // ============================================================

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            var regexMap = _tokenRegexMapService.GetMap();
            _tokenRegexMap = regexMap?.TokenRegex
                             ?? new Dictionary<string, TokenRegexDTO>(StringComparer.OrdinalIgnoreCase);

            _disciplineMap = _disciplineMapService.GetMap() ?? new DisciplineMapDTO();
            _entityMap = _entityMapService.GetMap() ?? new EntityMapDTO();

            _initialized = true;
        }

        // ============================================================
        //  BASE TOKENS
        // ============================================================

        private void DetectBaseTokens(string[] parts, TokenizationContext context)
        {
            if (parts is null || parts.Length == 0)
                return;

            // group base regex definitions by Position
            var baseByPos = _tokenRegexMap
                .Where(kv => kv.Value != null &&
                             kv.Value.Type.Equals("base", StringComparison.OrdinalIgnoreCase))
                .GroupBy(kv => kv.Value.Position)
                .ToDictionary(g => g.Key, g => g.ToList());

            var maxBasePos = _tokenRegexMapService.GetHighestBasePosition();

            for (int index = 0; index < parts.Length; index++)
            {
                var value = parts[index];
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                // beyond base area → suffix/exception only
                if (index > maxBasePos)
                    break;

                // what base slot is expected here? (Plant, PlantUnit, PlantSection, Equipment, Component, ...)
                var baseSlot = _tokenRegexMapService.GetTokenForPosition(index);
                if (baseSlot == null || string.IsNullOrWhiteSpace(baseSlot.Name))
                    continue;

                context.Tokens.TryGetValue(baseSlot.Name, out var existing);
                bool alreadyValid =
                    existing != null &&
                    !existing.IsMissing &&
                    !string.IsNullOrWhiteSpace(existing.Value) &&
                    !existing.Value.StartsWith("MISSING_", StringComparison.OrdinalIgnoreCase);

                // Codification-first: do not override valid codification result
                if (alreadyValid)
                    continue;

                if (!baseByPos.TryGetValue(index, out var candidates) || candidates.Count == 0)
                {
                    // we have no regex at all for this slot → if no token, mark missing
                    if (existing == null)
                    {
                        var missing = new Token
                        {
                            Key = baseSlot.Name,
                            Value = $"MISSING_{baseSlot.Name.ToUpperInvariant()}",
                            Type = "base",
                            Position = index,
                            IsMissing = true,
                            SourceMapKey = baseSlot.Name,
                            Note = $"RegexFallback: no regex candidates for base '{baseSlot.Name}' at position {index}."
                        };

                        context.Tokens[baseSlot.Name] = missing;
                        AddScore(context, baseSlot.Name, -10);
                        context.AddWarning(
                            $"⚠️ RegexFallback: no regex candidates for base '{baseSlot.Name}' at position {index}.");
                    }

                    continue;
                }

                // 1) primary candidate: regex definition with same key as baseSlot.Name
                var primary = candidates.FirstOrDefault(c =>
                    c.Key.Equals(baseSlot.Name, StringComparison.OrdinalIgnoreCase));

                if (!primary.Equals(default(KeyValuePair<string, TokenRegexDTO>)) &&
                    !string.IsNullOrWhiteSpace(primary.Value.Pattern) &&
                    Regex.IsMatch(value, $"^{primary.Value.Pattern}$", RegexOptions.IgnoreCase))
                {
                    var token = new Token
                    {
                        Key = baseSlot.Name,
                        Value = value,
                        Type = "base",
                        Position = index,
                        Pattern = primary.Value.Pattern,
                        IsMatch = true,
                        IsFallback = !alreadyValid,
                        SourceMapKey = primary.Key,
                        Note = !alreadyValid
                            ? $"RegexFallback: matched base '{baseSlot.Name}' = '{value}' at pos {index}."
                            : $"RegexFallback: confirmed base '{baseSlot.Name}' = '{value}' by regex at pos {index}."
                    };

                    context.Tokens[baseSlot.Name] = token;
                    AddScore(context, baseSlot.Name, !alreadyValid ? 20 : 5);

                    if (!alreadyValid)
                    {
                        context.AddMessage(
                            $"ℹ️ RegexFallback: base '{baseSlot.Name}' resolved as '{value}' (position {index}).");
                    }

                    continue;
                }

                // 2) fallback candidates: alternative keys at this position
                var fallback = candidates.FirstOrDefault(c =>
                    !c.Key.Equals(baseSlot.Name, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(c.Value.Pattern) &&
                    Regex.IsMatch(value, $"^{c.Value.Pattern}$", RegexOptions.IgnoreCase));

                if (!fallback.Equals(default(KeyValuePair<string, TokenRegexDTO>)))
                {
                    var tokKey = fallback.Key;

                    var token = new Token
                    {
                        Key = tokKey,
                        Value = value,
                        Type = "base",
                        Position = index,
                        Pattern = fallback.Value.Pattern,
                        IsMatch = true,
                        IsReplacement = true,
                        ReplacesKey = baseSlot.Name,
                        IsFallback = true,
                        SourceMapKey = fallback.Key,
                        Note =
                            $"RegexFallback: fallback '{tokKey}' matched '{value}' at pos {index}, replacing expected '{baseSlot.Name}'."
                    };

                    context.Tokens[tokKey] = token;

                    AddScore(context, tokKey, 15);
                    context.AddMessage(
                        $"ℹ️ RegexFallback: used fallback base '{tokKey}' for '{value}' (replaces {baseSlot.Name} at pos {index}).");

                    continue;
                }

                // 3) nothing matched → ensure missing marker present
                if (existing == null || !existing.IsMissing)
                {
                    var missing = new Token
                    {
                        Key = baseSlot.Name,
                        Value = $"MISSING_{baseSlot.Name.ToUpperInvariant()}",
                        Type = "base",
                        Position = index,
                        IsMissing = true,
                        SourceMapKey = baseSlot.Name,
                        Note =
                            $"RegexFallback: no regex match for base '{baseSlot.Name}' at pos {index} (value '{value}')."
                    };

                    context.Tokens[baseSlot.Name] = missing;
                }

                AddScore(context, baseSlot.Name, -10);
                context.AddWarning(
                    $"⚠️ RegexFallback: could not map '{value}' to base '{baseSlot.Name}' at position {index}.");
            }
        }

        // ============================================================
        //  SUFFIX TOKENS + EXCEPTIONS / FALLBACK
        // ============================================================

        private void DetectSuffixTokens(string[] parts, TokenizationContext context)
        {
            if (parts is null || parts.Length == 0)
                return;

            // 1) Discipline & Entity codes from their maps
            var disciplineCodes = _disciplineMap.Disciplines
                .SelectMany(d => new[] { d.Key, d.Value.Code })
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var entityCodes = _entityMap.Entities
                .SelectMany(e => new[] { e.Key, e.Value.Code })
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 2) Suffix definitions (including TagIncremental, TagComposite, PlantLayout*)
            var suffixDefs = _tokenRegexMap
                .Where(kv =>
                    kv.Value != null &&
                    kv.Value.Type.Equals("suffix", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

            // 3) base definitions by position
            var baseByPos = _tokenRegexMap
                .Where(kv =>
                    kv.Value != null &&
                    kv.Value.Type.Equals("base", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kv => kv.Value.Position, kv => kv.Key);

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

            // 4) prevent suffixes from consuming already recognized base values
            var currentBaseValues = context.Tokens.Values
                .Where(t => string.Equals(t.Type, "base", StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (suffixKey, def) in suffixDefs)
            {
                if (string.IsNullOrWhiteSpace(def.Pattern))
                    continue;

                // Discipline / Entity handled separately via their maps
                if (suffixKey.Equals("Discipline", StringComparison.OrdinalIgnoreCase))
                {
                    TryAddDisciplineToken(context, disciplineCodes, def, parts);
                    continue;
                }

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

                    // do not reuse values already used as base
                    if (currentBaseValues.Contains(p))
                        continue;

                    var replacesBase = FindBaseToReplace(def.Position);
                    if (string.IsNullOrWhiteSpace(replacesBase))
                        continue;

                    // replace only if base is missing / invalid
                    bool baseValid = context.Tokens.TryGetValue(replacesBase, out var baseTok)
                                     && baseTok != null
                                     && !baseTok.IsMissing
                                     && !string.IsNullOrWhiteSpace(baseTok.Value)
                                     && !baseTok.Value.StartsWith("MISSING_", StringComparison.OrdinalIgnoreCase);
                    if (baseValid)
                        continue;

                    // default target is the base slot we conceptually replace
                    var targetKey = replacesBase;

                    // TagIncremental: lands on "next" base (e.g. Component) if present
                    if (suffixKey.Equals("TagIncremental", StringComparison.OrdinalIgnoreCase))
                    {
                        var nextBase = FindNextBase(def.Position);
                        if (!string.IsNullOrWhiteSpace(nextBase))
                            targetKey = nextBase;
                    }

                    var targetPos = GetBasePos(targetKey);

                    var isException = IsExceptionSuffix(suffixKey);

                    var token = new Token
                    {
                        Key = targetKey,
                        Value = p,
                        Type = def.Type,
                        Pattern = def.Pattern,
                        Position = targetPos,
                        IsMatch = true,
                        IsReplacement = true,
                        ReplacesKey = replacesBase,
                        ReplacedBy = suffixKey,
                        SourceMapKey = suffixKey,
                        IsFallback = isException,
                        Note = BuildSuffixNote(suffixKey, replacesBase, targetKey, p, isException)
                    };

                    // write/overwrite under base key
                    context.Tokens[targetKey] = token;
                    currentBaseValues.Add(p);

                    // scoring & messaging
                    if (isException)
                    {
                        // PlantSection-level exception fallback
                        AddScore(context, targetKey, 15);
                        context.AddMessage(
                            $"ℹ️ RegexFallback: exception '{suffixKey}' with value '{p}' used as fallback for missing {replacesBase} → {targetKey}.");
                    }
                    else if (suffixKey.Equals("TagIncremental", StringComparison.OrdinalIgnoreCase))
                    {
                        AddScore(context, targetKey, 10);
                        context.AddMessage(
                            $"ℹ️ RegexFallback: TagIncremental '{p}' attached to '{targetKey}' (replaces {replacesBase}).");
                    }
                    else
                    {
                        AddScore(context, targetKey, 8);
                    }
                }
            }

            // Merge TagComposite parts if present (we do NOT synthesize TagComposite)
            if (suffixDefs.TryGetValue("TagComposite", out var compDef)
                && context.Tokens.ContainsKey("TagComposite"))
            {
                var compositeParts = context.Tokens.Values
                    .Where(t => t.Key.Equals("TagComposite", StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (compositeParts.Count > 1)
                {
                    context.Tokens["TagComposite"] = new Token
                    {
                        Key = "TagComposite",
                        Value = string.Join("-", compositeParts),
                        Type = compDef.Type,
                        Pattern = compDef.Pattern,
                        Position = compDef.Position,
                        IsMatch = true,
                        SourceMapKey = "TagComposite",
                        Note = $"Merged {compositeParts.Count} TagComposite segments."
                    };
                }
            }
        }

        // ============================================================
        //  Discipline / Entity helpers
        // ============================================================

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
                    Position = int.MaxValue,
                    Note = $"Matched discipline code '{part}'."
                };

                AddScore(context, "Discipline", 20);
                context.AddMessage($"ℹ️ RegexFallback: detected discipline '{part}'.");
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
                    Position = int.MaxValue,
                    Note = $"Matched entity code '{part}'."
                };

                AddScore(context, "Entity", 20);
                context.AddMessage($"ℹ️ RegexFallback: detected entity '{part}'.");
                return;
            }
        }

        // ============================================================
        //  Fallback / exception helpers
        // ============================================================

        /// <summary>
        /// PlantSection-level exception suffixes:
        /// - PlantLayout* (BUILDINGS, WALKWAYS, STRIP, OPERATING, etc.)
        /// - MANDUMMY, LIFTCAR (planned)
        /// Extend here as you add more config keys.
        /// </summary>
        private static bool IsExceptionSuffix(string suffixKey)
        {
            if (string.IsNullOrWhiteSpace(suffixKey))
                return false;

            if (suffixKey.StartsWith("PlantLayout", StringComparison.OrdinalIgnoreCase))
                return true;

            return suffixKey.Equals("MANDUMMY", StringComparison.OrdinalIgnoreCase)
                   || suffixKey.Equals("LIFTCAR", StringComparison.OrdinalIgnoreCase);
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

        // ============================================================
        //  Scoring helper
        // ============================================================

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
