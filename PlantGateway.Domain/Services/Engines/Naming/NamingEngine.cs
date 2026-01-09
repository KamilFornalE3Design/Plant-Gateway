using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.Extensions;
using SMSgroup.Aveva.Utilities.Helpers;

namespace SMSgroup.Aveva.Utilities.Engines
{
    /// <summary>
    /// Builds the BaseName for an element using RoleEngineResult and TechnicalOrderStructureMap.
    /// BaseName excludes discipline/entity suffixes.
    /// </summary>
    public sealed class NamingEngine : IEngine
    {
        private readonly IDisciplineHierarchyTokenMapService _disciplineHierarchyTokenMapService;
        private readonly ITokenRegexMapService _tokenRegexMapService;

        private readonly object _syncRoot = new();

        public NamingEngine(IDisciplineHierarchyTokenMapService disciplineHierarchyTokenMapService, ITokenRegexMapService tokenRegexMapService)
        {
            _disciplineHierarchyTokenMapService = disciplineHierarchyTokenMapService ?? throw new ArgumentNullException(nameof(disciplineHierarchyTokenMapService));
            _tokenRegexMapService = tokenRegexMapService ?? throw new ArgumentNullException(nameof(tokenRegexMapService));

            Init();
        }

        private void Init()
        {
            lock (_syncRoot)
            {
                // For now nothing
            }
        }

        public NamingEngineResult Process(TakeOverPointDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var baseName = dto.AvevaTag ?? string.Empty;
            var normalizedBase = baseName.Replace('.', '_').Replace('-', '_');
            var isValid = !string.IsNullOrWhiteSpace(baseName);

            var result = new NamingEngineResult
            {
                SourceDtoId = dto.Id,
                BaseName = baseName,
                NormalizedBaseName = normalizedBase,
                TokensUsed = new List<string> { "AvevaTag" },
                IsValid = isValid
            }.Also(r => r.AddMessage(isValid ? $"✅ BaseName resolved from AVEVA_TAG → {baseName}" : $"⚠️ NamingEngine: Missing or invalid AVEVA_TAG"));

            return result;
        }

        public NamingEngineResult Execute(ProjectStructureDTO dto)
        {
            var context = new NamingContext { Dto = dto };

            PreProcess(context);
            Process(context);
            PostProcess(context);

            return BuildResult(context);
        }
        /// <summary>
        /// Collects input DTO, TokenEngine results, and TokenRegex map definitions
        /// for subsequent naming generation.
        /// </summary>
        private void PreProcess(NamingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var dto = context.Dto ?? throw new ArgumentNullException(nameof(context.Dto));

            // === 1️⃣ Get TokenEngine result ===
            context.TokenResult = dto.EngineResults.OfType<TokenEngineResult>().FirstOrDefault();

            if (context.TokenResult == null)
                throw new InvalidOperationException($"❌ Missing TokenEngineResult for DTO {dto.Id}");

            // === 2️⃣ Load map & base order ===
            var map = _tokenRegexMapService.GetMap();
            context.TokenDefinitions = map.TokenRegex ?? new Dictionary<string, TokenRegexDTO>(StringComparer.OrdinalIgnoreCase);
            context.DefaultOrder = _tokenRegexMapService.GetDefaultNameOrder() ?? new List<string>();

            // === 3️⃣ Copy tokens (typed) ===
            context.Tokens = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);

            if (context.TokenResult.Tokens != null)
            {
                foreach (var kv in context.TokenResult.Tokens)
                {
                    // Defensive copy — ensure Token instance not null
                    if (kv.Value != null)
                        context.Tokens[kv.Key] = kv.Value;
                }
            }
        }
        /// <summary>
        /// Builds ordered token sequence from TokenEngineResult, using only base and valid replacement tokens.
        /// Excludes discipline/entity and suffix-only entries.
        /// </summary>
        private void Process(NamingContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var defs = context.TokenDefinitions ?? new(StringComparer.OrdinalIgnoreCase);
            var order = context.DefaultOrder ?? new List<string>();
            var tokens = context.Tokens ?? new(StringComparer.OrdinalIgnoreCase);

            context.OrderedTokens.Clear();

            // Guards: avoid using the same *suffix kind* (e.g., TagIncremental) more than once
            var usedSuffixKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Build ordered sequence strictly by base order
            foreach (var baseKey in order)
            {
                if (!tokens.TryGetValue(baseKey, out var token) || token == null)
                    continue;

                // Skip invalid/missing
                if (token.IsMissing || string.IsNullOrWhiteSpace(token.Value) ||
                    token.Value.StartsWith("MISSING_", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Skip pure suffix tokens (only allow suffix if it *replaced a base* and is stored under the target base)
                if (string.Equals(token.Type, "suffix", StringComparison.OrdinalIgnoreCase) && !token.IsReplacement)
                    continue;

                // If it's a replacement, enforce "use once per suffix kind" rule (e.g., TagIncremental)
                if (token.IsReplacement && !string.IsNullOrWhiteSpace(token.ReplacedBy))
                {
                    if (usedSuffixKinds.Contains(token.ReplacedBy))
                    {
                        context.Warnings.Add($"⚠️ Suffix '{token.ReplacedBy}' already used — skipping duplicate value '{token.Value}'.");
                        continue;
                    }

                    usedSuffixKinds.Add(token.ReplacedBy);
                }

                // IMPORTANT:
                // Only place the token if its *target base* equals the current baseKey.
                // This prevents cross-level duplicates (e.g., the same token being placed under both Equipment and Component).
                if (!string.Equals(token.Key, baseKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                context.OrderedTokens[baseKey] = token;
            }

            // NO: do not mirror replacements under ReplacesKey — that causes duplicates
            // (Removed the old loop that did: context.OrderedTokens[token.ReplacesKey] = token)

            // 🧩 Diagnostic summary
            var orderedValues = context.OrderedTokens.Values.Select(t => t.Value);
            if (orderedValues.Any())
                context.Messages.Add($"Process built ordered tokens: {string.Join('.', orderedValues)}");
            else
                context.Warnings.Add("⚠ No base tokens processed.");
        }


        /// <summary>
        /// Finalizes the BaseName by concatenating ordered tokens using SeparatorHelper.
        /// Excludes discipline/entity and builds normalized variant.
        /// </summary>
        private void PostProcess(NamingContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var baseTokens = context.OrderedTokens.Values.ToList();
            var baseValues = baseTokens
                .Where(t => !string.IsNullOrWhiteSpace(t.Value))
                .Select(t => t.Value)
                .ToArray();

            var baseName = SeparatorHelper.JoinAuto(baseValues);
            var normalizedBase = SeparatorHelper.Replace(baseName, SeparatorHelper.Normalized);

            bool isValid = baseValues.Length > 0 && !baseValues.Any(v => v.StartsWith("MISSING_", StringComparison.OrdinalIgnoreCase));

            context.BaseName = baseName;
            context.IsComplete = isValid;

            if (!isValid)
                context.Warnings.Add("⚠ Missing or invalid base tokens detected while building BaseName.");

            context.Messages.Add($"🏷 Base name built: {baseName}");
        }

        /// <summary>
        /// Converts the internal NamingContext into the final NamingEngineResult.
        /// </summary>
        private NamingEngineResult BuildResult(NamingContext context)
        {
            var result = new NamingEngineResult
            {
                SourceDtoId = context.Dto.Id,
                BaseName = context.BaseName,
                NormalizedBaseName = context.BaseName.Replace('.', '_'),
                IsValid = context.IsComplete,
                // Use the token's MappingSummary directly — no extra base key prefix
                TokensUsed = context.OrderedTokens
                                            .Select(kv => kv.Value.MappingSummary)
                                            .ToList(),
                Message = new List<string>(context.Messages),
                Warning = new List<string>(context.Warnings),
                Error = new List<string>()
            };

            result.AddMessage(result.IsValid
                ? "✅ NamingEngine completed successfully."
                : "⚠️ NamingEngine completed with warnings.");

            return result;
        }

        //public NamingEngineResult Process(ProjectStructureDTO dto)
        //{
        //    if (dto == null)
        //        throw new ArgumentNullException(nameof(dto));

        //    // === 1️⃣ Retrieve prerequisites ===
        //    var tokenResult = dto.EngineResults.OfType<TokenEngineResult>().FirstOrDefault();
        //    var roleResult = dto.EngineResults.OfType<RoleEngineResult>().FirstOrDefault();
        //    var disciplineResult = dto.EngineResults.OfType<DisciplineEngineResult>().FirstOrDefault();

        //    if (tokenResult == null)
        //        throw new InvalidOperationException("❌ TokenEngineResult missing before NamingEngine.");
        //    if (roleResult == null)
        //        throw new InvalidOperationException("❌ RoleEngineResult missing before NamingEngine.");

        //    var role = roleResult.AvevaType;
        //    var discipline = disciplineResult?.Discipline ?? string.Empty;

        //    // === 🧩 Handle empty role fallback ===
        //    if (string.IsNullOrWhiteSpace(role))
        //    {
        //        return new NamingEngineResult
        //        {
        //            SourceDtoId = dto.Id,
        //            BaseName = string.Empty,
        //            NormalizedBaseName = string.Empty,
        //            TokensUsed = new List<string>(),
        //            IsValid = false,
        //            Message = new List<string>() // initialize list
        //        }.Also(r => r.AddWarning("⚠️ NamingEngine: Role is empty — skipping TOS structure lookup."));
        //    }

        //    var disciplineMap = _disciplineHierarchyTokenMapService.GetMap().Disciplines;
        //    var tokenRegexMap = _tokenRegexMapService.GetMap().TokenRegex;
        //    var tokens = tokenResult.Tokens;

        //    var selectedKey = DetectContext(tokenResult, discipline);

        //    if (!disciplineMap.TryGetValue(selectedKey, out var disciplineDef))
        //        disciplineDef = disciplineMap["DEFAULT"];

        //    if (!disciplineDef.Tokens.TryGetValue(role, out var schema))
        //        throw new InvalidOperationException($"❌ No token schema found for role '{role}' in discipline '{discipline}'.");

        //    // === 3️⃣ Collect only BASE tokens for this role ===
        //    var tokensToUse = schema.Base
        //        ?.Where(t => !string.Equals(t, "Discipline", StringComparison.OrdinalIgnoreCase)
        //                  && !string.Equals(t, "Entity", StringComparison.OrdinalIgnoreCase))
        //        .ToList()
        //        ?? new List<string>();

        //    // === 4️⃣ Build name ===
        //    var values = new List<string>();
        //    foreach (var token in tokensToUse)
        //    {
        //        if (tokens.TryGetValue(token, out var value) && !string.IsNullOrWhiteSpace(value))
        //            values.Add(value);
        //        else
        //            values.Add($"MISSING_{token.ToUpperInvariant()}");
        //    }

        //    // Unified separator logic
        //    var baseName = SeparatorHelper.JoinWith("Equipment", values.ToArray());
        //    var normalizedBaseName = SeparatorHelper.Replace(baseName, SeparatorHelper.Normalized);
        //    bool isValid = !values.Any(v => v.StartsWith("MISSING_", StringComparison.OrdinalIgnoreCase));

        //    // === 5️⃣ Return result ===
        //    return new NamingEngineResult
        //    {
        //        SourceDtoId = dto.Id,
        //        BaseName = baseName,
        //        NormalizedBaseName = normalizedBaseName,
        //        TokensUsed = tokensToUse,
        //        IsValid = isValid,
        //        Message = new List<string>() // initialize the list properly
        //    }.Also(r => r.AddMessage(isValid ? $"✅ BaseName built for role '{role}' → {baseName}" : $"⚠️ BaseName built with missing tokens for role '{role}' → {baseName}"))
        //        .When(r => !isValid, r => r.AddWarning($"⚠️ Some required tokens were null while building BaseName for '{role}'."));
        //}


        /// <summary>
        /// Detects business context (hierarchy type) based on available token flags and discipline.
        /// Returns the resolved hierarchy key: "DEFAULT" or the discipline ("ST"/"CI").
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
                // Default: Equipment-level structure
                selectedKey = "DEFAULT";
                //result.AddMessage("🏗 Context detected: Equipment-level structure → DEFAULT hierarchy.");
            }
            else if (hasPlantSection && !hasEquipment && hasIncremental && (discipline == "ST" || discipline == "CI"))
            {
                // Structural or civil section-level structure
                selectedKey = discipline;
                //result.AddMessage($"🏗 Context detected: Section-level structure for {discipline} → {discipline} hierarchy.");
            }
            else
            {
                // Fallback
                selectedKey = "DEFAULT";
                //result.AddMessage("🏗 Context fallback: DEFAULT hierarchy applied.");
            }

            return selectedKey;
        }


        internal sealed class NamingContext
        {
            // Input
            public ProjectStructureDTO Dto { get; set; }
            public TokenEngineResult TokenResult { get; set; }

            // Map + definitions
            public IReadOnlyList<string> DefaultOrder { get; set; } = new List<string>();
            public Dictionary<string, TokenRegexDTO> TokenDefinitions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, Token> Tokens { get; set; } = new(StringComparer.OrdinalIgnoreCase);

            // Output building
            public Dictionary<string, Token> OrderedTokens { get; set; } = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);
            public string BaseName { get; set; } = string.Empty;
            public bool IsComplete { get; set; }
            public List<string> Messages { get; set; } = new();
            public List<string> Warnings { get; set; } = new();

            public string InputValue => TokenResult?.RawInputValue ?? string.Empty;
        }
    }
}
