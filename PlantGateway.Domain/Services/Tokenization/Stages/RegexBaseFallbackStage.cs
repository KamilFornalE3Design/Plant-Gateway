using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.Tokenization;
using System.Text.RegularExpressions;

namespace PlantGateway.Domain.Services.Tokenization.Stages
{
    /// <summary>
    /// Stage that performs regex-based detection of base-level tokens
    /// (Plant, PlantUnit, PlantSection, Equipment, Component) as a
    /// fallback when codification could not fully resolve the structure.
    ///
    /// Design:
    /// - Never overwrites a valid codification token (codification-first rule).
    /// - Uses TokenRegexMap "base" definitions grouped by Position.
    /// - Creates MISSING_* tokens when nothing matches for a base slot.
    /// - Emits codification-aware info/warn messages and adjusts scoring.
    /// </summary>
    public sealed class RegexBaseFallbackStage : ITokenizationStage
    {
        public TokenizationStageId Id => TokenizationStageId.RegexBaseFallback;

        private readonly IReadOnlyDictionary<string, TokenRegexDTO> _tokenRegexMap;
        private readonly HashSet<string> _excludedKeys;

        // Cached helpers: base definitions grouped by position
        private readonly Dictionary<int, List<KeyValuePair<string, TokenRegexDTO>>> _baseByPosition;
        private readonly int _maxBasePosition;

        public RegexBaseFallbackStage(
            IReadOnlyDictionary<string, TokenRegexDTO> tokenRegexMap,
            HashSet<string> excludedKeys)
        {
            _tokenRegexMap = tokenRegexMap ?? throw new ArgumentNullException(nameof(tokenRegexMap));
            _excludedKeys = excludedKeys ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            _baseByPosition = _tokenRegexMap
                .Where(kv =>
                    kv.Value != null &&
                    kv.Value.Type != null &&
                    kv.Value.Type.Equals("base", StringComparison.OrdinalIgnoreCase) &&
                    kv.Value.Position >= 0)
                .GroupBy(kv => kv.Value.Position)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList(),
                    comparer: EqualityComparer<int>.Default);

            _maxBasePosition = _baseByPosition.Count == 0
                ? -1
                : _baseByPosition.Keys.Max();
        }

        public void Execute(TokenizationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // If we have no regex base definitions, nothing to do.
            if (_maxBasePosition < 0)
            {
                context.AddMessage("ℹ️ RegexBaseFallback: no base definitions in TokenRegexMap – skipping stage.");
                return;
            }

            // Ensure parts are available (PreProcessing should have done this)
            var parts = context.Parts;
            if (parts == null || parts.Length == 0)
            {
                var source = !string.IsNullOrWhiteSpace(context.NormalizedInput)
                    ? context.NormalizedInput
                    : context.RawInput?.Trim().ToUpperInvariant() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(source))
                {
                    context.AddWarning("⚠️ RegexBaseFallback: no input available for regex processing.");
                    return;
                }

                var normalized = NormalizeSeparators(source);

                parts = normalized.Split('_', StringSplitOptions.RemoveEmptyEntries);
                context.NormalizedInput = normalized;
                context.Parts = parts;

                context.AddMessage(
                    $"ℹ️ RegexBaseFallback: rebuilt {parts.Length} segments from input '{normalized}'.");
            }

            if (parts.Length == 0)
            {
                context.AddWarning("⚠️ RegexBaseFallback: normalized input produced zero segments.");
                return;
            }

            // Process each part in base range
            for (var index = 0; index < parts.Length; index++)
            {
                if (index > _maxBasePosition)
                    break; // beyond base slots → suffix/exception stage will handle

                var value = parts[index]?.Trim();
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                var baseKey = GetExpectedBaseKeyForPosition(index);
                if (string.IsNullOrWhiteSpace(baseKey))
                    continue;

                // Respect codification-first: do not override valid structural tokens
                if (context.Tokens.TryGetValue(baseKey, out var existing) &&
                    existing != null &&
                    !existing.IsMissing &&
                    !string.IsNullOrWhiteSpace(existing.Value) &&
                    !existing.Value.StartsWith("MISSING_", StringComparison.OrdinalIgnoreCase))
                {
                    // Optional: we could confirm codification against regex and score,
                    // but for now we simply trust codification here.
                    continue;
                }

                if (!_baseByPosition.TryGetValue(index, out var candidates) || candidates.Count == 0)
                {
                    // No regex candidate at this position → mark missing if nothing exists
                    if (!context.Tokens.ContainsKey(baseKey))
                    {
                        var missingToken = new Token
                        {
                            Key = baseKey,
                            Value = $"MISSING_{baseKey.ToUpperInvariant()}",
                            Type = "base",
                            Position = index,
                            IsMissing = true,
                            SourceMapKey = baseKey,
                            Note = $"RegexBaseFallback: no regex candidates defined for base '{baseKey}' at position {index}."
                        };

                        context.Tokens[baseKey] = missingToken;
                        AddScore(context, baseKey, -10);
                        context.AddWarning(
                            $"⚠️ RegexBaseFallback: no regex candidates found for base '{baseKey}' at position {index}.");
                    }

                    continue;
                }

                // 1) Primary rule: definition whose key equals the expected base key
                var primary = candidates.FirstOrDefault(kv =>
                    kv.Key.Equals(baseKey, StringComparison.OrdinalIgnoreCase));

                if (!primary.Equals(default(KeyValuePair<string, TokenRegexDTO>)) &&
                    !string.IsNullOrWhiteSpace(primary.Value.Pattern) &&
                    Regex.IsMatch(value, $"^{primary.Value.Pattern}$", RegexOptions.IgnoreCase))
                {
                    var token = new Token
                    {
                        Key = baseKey,
                        Value = value,
                        Type = "base",
                        Position = index,
                        Pattern = primary.Value.Pattern,
                        IsMatch = true,
                        IsFallback = true,
                        SourceMapKey = primary.Key,
                        Note = $"RegexBaseFallback: matched base '{baseKey}' = '{value}' at position {index}."
                    };

                    context.Tokens[baseKey] = token;
                    AddScore(context, baseKey, 15);
                    context.AddMessage(
                        $"ℹ️ RegexBaseFallback: resolved base '{baseKey}' as '{value}' (position {index}).");

                    continue;
                }

                // 2) Fallback rule: alternative base definitions at the same position
                var fallback = candidates.FirstOrDefault(kv =>
                    !kv.Key.Equals(baseKey, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(kv.Value.Pattern) &&
                    Regex.IsMatch(value, $"^{kv.Value.Pattern}$", RegexOptions.IgnoreCase));

                if (!fallback.Equals(default(KeyValuePair<string, TokenRegexDTO>)))
                {
                    var fallbackKey = fallback.Key;

                    var token = new Token
                    {
                        Key = fallbackKey,
                        Value = value,
                        Type = "base",
                        Position = index,
                        Pattern = fallback.Value.Pattern,
                        IsMatch = true,
                        IsReplacement = true,
                        ReplacesKey = baseKey,
                        IsFallback = true,
                        SourceMapKey = fallbackKey,
                        Note =
                            $"RegexBaseFallback: fallback base '{fallbackKey}' matched '{value}' at position {index}, replacing expected '{baseKey}'."
                    };

                    context.Tokens[fallbackKey] = token;
                    AddScore(context, fallbackKey, 12);
                    context.AddMessage(
                        $"ℹ️ RegexBaseFallback: used fallback base '{fallbackKey}' for '{value}' (replaces {baseKey} at pos {index}).");

                    continue;
                }

                // 3) Nothing matched → ensure MISSING_* marker present
                if (!context.Tokens.TryGetValue(baseKey, out var missingExisting) ||
                    !missingExisting.IsMissing)
                {
                    var missingToken = new Token
                    {
                        Key = baseKey,
                        Value = $"MISSING_{baseKey.ToUpperInvariant()}",
                        Type = "base",
                        Position = index,
                        IsMissing = true,
                        SourceMapKey = baseKey,
                        Note =
                            $"RegexBaseFallback: no regex match for base '{baseKey}' at position {index} (value '{value}')."
                    };

                    context.Tokens[baseKey] = missingToken;
                }

                AddScore(context, baseKey, -10);
                context.AddWarning(
                    $"⚠️ RegexBaseFallback: could not map '{value}' to base '{baseKey}' at position {index}.");
            }

            context.AddMessage(
                $"🔎 RegexBaseFallback: completed base regex evaluation. Tokens now: {context.Tokens.Count}.");
        }

        /// <summary>
        /// Returns the canonical base key for a given position.
        /// Prefers well-known keys like Plant, PlantUnit, PlantSection, Equipment, Component
        /// if multiple definitions share the same Position.
        /// </summary>
        private string? GetExpectedBaseKeyForPosition(int position)
        {
            if (!_baseByPosition.TryGetValue(position, out var candidates) || candidates.Count == 0)
                return null;

            // Preferred ordering for canonical base keys
            var preferredOrder = new[]
            {
                "Plant",
                "PlantUnit",
                "PlantSection",
                "Equipment",
                "Component"
            };

            foreach (var key in preferredOrder)
            {
                if (candidates.Any(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
                    return key;
            }

            // Fallback: first candidate key
            return candidates[0].Key;
        }

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
        private static string NormalizeSeparators(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return string.Empty;

            // 1) Trim + upper
            var trimmed = source.Trim().ToUpperInvariant();

            // 2) Replace typical separators (space, -, ., /, \, :, ;) with "_"
            return Regex.Replace(trimmed, @"[\s\-\./\\:;]+", "_");
        }
    }
}
