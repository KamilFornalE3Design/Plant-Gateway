using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.Tokenization;

namespace PlantGateway.Domain.Services.Tokenization.Stages
{
    /// <summary>
    /// Final stage of the tokenization pipeline.
    ///
    /// Responsibilities:
    /// - Safety-check that we actually have tokens.
    /// - Separate excluded/useless tokens into <see cref="TokenizationContext.ExcludedTokens"/>.
    /// - Order valid tokens by Position (then Key).
    /// - Run token order consistency check against TokenRegexMap positions.
    /// - Compute IsValid / IsConsistencyChecked flags and add summary messages.
    /// </summary>
    public sealed class PostProcessingStage : ITokenizationStage
    {
        public TokenizationStageId Id => TokenizationStageId.PostProcessing;

        private readonly IReadOnlyDictionary<string, TokenRegexDTO> _tokenRegexMap;

        public PostProcessingStage(IReadOnlyDictionary<string, TokenRegexDTO> tokenRegexMap)
        {
            _tokenRegexMap = tokenRegexMap ?? throw new ArgumentNullException(nameof(tokenRegexMap));
        }

        public void Execute(TokenizationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var tokens = context.Tokens ?? new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);

            // 🧩 STEP 1: SAFETY CHECK
            if (tokens.Count == 0)
            {
                context.AddWarning("⚠️ PostProcessing: no tokens found to post-process.");
                context.IsValid = false;
                context.IsConsistencyChecked = false;
                return;
            }

            // 🧱 STEP 2: SEPARATE EXCLUDED TOKENS
            Dictionary<string, Token> valid;
            Dictionary<string, Token> excluded;

            try
            {
                (valid, excluded) = SeparateExcludedTokens(tokens);
            }
            catch (Exception ex)
            {
                context.AddWarning($"⚠️ PostProcessing: error while separating excluded tokens: {ex.Message}");
                valid = tokens;
                excluded = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);
            }

            context.Tokens = valid;
            context.ExcludedTokens.Clear();
            foreach (var kv in excluded)
                context.ExcludedTokens[kv.Key] = kv.Value;

            // 🧾 STEP 3: ORDER VALID TOKENS
            try
            {
                context.Tokens = OrderTokensByPosition(context.Tokens);
            }
            catch (Exception ex)
            {
                context.AddWarning($"⚠️ PostProcessing: error while ordering tokens: {ex.Message}");
            }

            // 🧬 STEP 4: RUN CONSISTENCY CHECK
            try
            {
                RunConsistencyCheck(context);
            }
            catch (Exception ex)
            {
                context.AddWarning($"⚠️ PostProcessing: consistency check failed: {ex.Message}");
            }

            // 🧮 STEP 5: COMPUTE FLAGS
            context.IsValid = context.Tokens.Values
                .All(t => !t.IsMissing && !string.IsNullOrWhiteSpace(t.Value));

            // IsConsistencyChecked is set inside RunConsistencyCheck when it actually runs
            if (!context.IsConsistencyChecked)
            {
                // If we could not run it for some reason, we still mark that post-process completed
                // but IsConsistencyChecked stays false so Result.IsSuccess remains honest.
                context.AddWarning("⚠️ PostProcessing: token consistency could not be fully verified.");
            }

            // 🧷 STEP 6: SUMMARIZE
            var hasErrors = context.Error.Count > 0;

            if (!hasErrors && context.IsValid && context.IsConsistencyChecked)
            {
                context.AddMessage($"✅ PostProcessing: {context.Tokens.Count} token(s) ready for downstream processing.");
            }
            else if (!context.IsValid)
            {
                context.AddWarning("⚠️ PostProcessing: detected invalid or missing tokens.");
            }

            // Log excluded tokens count for diagnostics
            if (context.ExcludedTokens.Any())
            {
                context.AddMessage($"🧹 PostProcessing: {context.ExcludedTokens.Count} token(s) moved to ExcludedTokens.");
            }
        }

        // =====================================================================
        //  SeparateExcludedTokens
        // =====================================================================

        /// <summary>
        /// Moves invalid, missing, or useless tokens into a separate dictionary.
        /// Returns a cleaned "valid" dictionary and an "excluded" dictionary.
        /// </summary>
        private static (Dictionary<string, Token> valid, Dictionary<string, Token> excluded)
            SeparateExcludedTokens(Dictionary<string, Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return (tokens ?? new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase),
                        new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase));

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
                    token.Value.StartsWith("MISSING_", StringComparison.OrdinalIgnoreCase) ||
                    !token.IsProcessable;

                if (isUseless)
                {
                    excluded[kv.Key] = token;
                    continue;
                }

                valid[kv.Key] = token;
            }

            return (valid, excluded);
        }

        // =====================================================================
        //  Ordering
        // =====================================================================

        /// <summary>
        /// Orders the token dictionary by Position (ascending), keeping tokens without
        /// positions at the end. Rebuilds dictionary in correct order for serialization.
        /// </summary>
        private static Dictionary<string, Token> OrderTokensByPosition(Dictionary<string, Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return tokens ?? new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);

            return tokens
                .OrderBy(kv => kv.Value.Position >= 0 ? kv.Value.Position : int.MaxValue)
                .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        }

        // =====================================================================
        //  Consistency check
        // =====================================================================

        /// <summary>
        /// Checks whether the observed token order is consistent with the expected
        /// sequence defined by TokenRegexMap positions.
        /// 
        /// This is a soft check:
        /// - On success → adds a positive message.
        /// - On mismatch → adds warnings; does not throw.
        /// </summary>
        private void RunConsistencyCheck(TokenizationContext context)
        {
            var tokens = context.Tokens;
            if (tokens == null || tokens.Count == 0)
                return;

            context.IsConsistencyChecked = true;

            // 1️⃣ Expected order from TokenRegexMap (only entries with Position >= 0)
            var expectedOrder = _tokenRegexMap
                .Where(kv => kv.Value != null && kv.Value.Position >= 0)
                .OrderBy(kv => kv.Value.Position)
                .Select(kv => kv.Key)
                .ToList();

            if (expectedOrder.Count == 0)
            {
                context.AddWarning("⚠️ PostProcessing: TokenRegexMap has no positional entries; skipping consistency check.");
                return;
            }

            // Build a fast lookup: token key -> expected index
            var expectedIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < expectedOrder.Count; i++)
                expectedIndexMap[expectedOrder[i]] = i;

            // 2️⃣ Current order of tokens (after ordering by Position)
            var currentOrder = tokens.Keys.ToList();

            var lastExpectedIndex = -1;
            var isConsistent = true;

            foreach (var tokenKey in currentOrder)
            {
                if (!expectedIndexMap.TryGetValue(tokenKey, out var expectedIndex))
                    continue; // unknown token, ignore for ordering purposes

                if (expectedIndex < lastExpectedIndex)
                {
                    isConsistent = false;
                    context.AddWarning($"⚠️ PostProcessing: token order inconsistency detected near '{tokenKey}'.");
                    break;
                }

                lastExpectedIndex = expectedIndex;
            }

            if (isConsistent)
            {
                context.AddMessage("✅ PostProcessing: token order consistency verified.");
            }
            else
            {
                context.AddWarning("⚠️ PostProcessing: token hierarchy order does not match expected sequence.");
            }
        }
    }
}
