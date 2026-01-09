using SMSgroup.Aveva.Config.Models.Tokenization;

namespace PlantGateway.Domain.Services.Tokenization.Stages
{
    /// <summary>
    /// First stage of the tokenization pipeline.
    /// 
    /// Responsibilities:
    /// - Validate that input is present.
    /// - Normalize raw input (trim, uppercase, separator unification).
    /// - Split into logical parts for subsequent stages.
    /// - Record a basic diagnostic message.
    /// 
    /// This stage does not depend on any external maps.
    /// </summary>
    public sealed class PreProcessingStage : ITokenizationStage
    {
        public TokenizationStageId Id => TokenizationStageId.PreProcessing;

        public void Execute(TokenizationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // If a previous stage (in some reused scenario) already produced
            // hard errors, do nothing. In your current pipeline this will
            // never trigger because PreProcessing is first, but it's safe.
            if (context.Error.Count > 0)
            {
                context.AddMessage("⏭️ PreProcessingStage skipped because context already contains errors.");
                return;
            }

            // 1) Validate input
            if (string.IsNullOrWhiteSpace(context.RawInput))
            {
                context.AddError("❌ Empty AvevaTag – tokenization aborted in PreProcessing.");
                return;
            }

            // 2) Normalize input
            //    - Trim whitespace
            //    - Uppercase for consistent comparisons
            //    - Replace '-', '.' with '_' for later splitting
            var input = context.RawInput.Trim().ToUpperInvariant();
            var normalized = input
                .Replace('-', '_')
                .Replace('.', '_');

            // 3) Split into parts (segments)
            var parts = normalized.Split(
                new[] { '_' },
                StringSplitOptions.RemoveEmptyEntries);

            // 4) Store back in context
            context.RawInput = input;
            context.NormalizedInput = normalized;
            context.Parts = parts;

            // NOTE:
            // Tokens / ExcludedTokens / Message / Warning / Error are
            // initialized empty on a fresh context. We don't clear them
            // here to avoid accidentally wiping messages in any reuse scenario.

            // 5) Diagnostics
            context.AddMessage(
                $"🧩 PreProcess: normalized input '{normalized}' into {parts.Length} segments.");
        }
    }
}
