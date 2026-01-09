using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Models.Disposition;
using SMSgroup.Aveva.Config.Models.EngineResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSgroup.Aveva.Utilities.Engines.Disposition.Stages
{
    /// <summary>
    /// Stage 10:
    /// - Basic input analysis (empty / whitespace tag detection),
    /// - Fallback name assignment for empty tags,
    /// - Initial token presence flag.
    /// 
    /// It does NOT decide the final bucket – it only prepares flags
    /// that later stages (quality + bucket assignment) will use.
    /// </summary>
    internal sealed class DispositionPreProcessingStage : IDispositionStage
    {
        public DispositionStageId Id => DispositionStageId.PreProcessing;

        public string Name => nameof(DispositionPreProcessingStage);

        public void Execute(DispositionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            ValidateTokenResult(context);

            SyncInputValues(context);
            DetectAndHandleEmptyTag(context);
            ComputeTokenPresence(context);

            // We intentionally do NOT touch IsValid / IsConsistencyChecked here.
        }

        #region === Private helpers ===

        private static void ValidateTokenResult(DispositionContext context)
        {
            if (context.TokenResult == null)
                throw new InvalidOperationException(
                    $"{nameof(DispositionPreProcessingStage)} requires a non-null {nameof(TokenEngineResult)} in the context.");
        }

        /// <summary>
        /// Ensures RawInputValue and NormalizedInputValue are populated,
        /// preferring values from TokenResult where available.
        /// </summary>
        private static void SyncInputValues(DispositionContext context)
        {
            // If RawInputValue is not set on the context yet, take it from TokenResult.
            if (string.IsNullOrWhiteSpace(context.RawInputValue))
            {
                context.RawInputValue = context.TokenResult.RawInputValue ?? string.Empty;
            }

            // Prefer normalized value from TokenEngine, but keep the option
            // to fall back to the raw value if it's missing.
            if (!string.IsNullOrWhiteSpace(context.TokenResult.NormalizedInputValue))
            {
                context.NormalizedInputValue = context.TokenResult.NormalizedInputValue;
            }
            else if (string.IsNullOrWhiteSpace(context.NormalizedInputValue) &&
                     !string.IsNullOrWhiteSpace(context.RawInputValue))
            {
                context.NormalizedInputValue = context.RawInputValue.Trim();
            }
        }

        /// <summary>
        /// Detects empty / whitespace tag and applies a fallback name if needed.
        /// Sets IsTagEmptyOrWhitespace and logs a warning.
        /// </summary>
        private static void DetectAndHandleEmptyTag(DispositionContext context)
        {
            context.IsTagEmptyOrWhitespace = string.IsNullOrWhiteSpace(context.RawInputValue);

            if (!context.IsTagEmptyOrWhitespace)
                return;

            // Basic fallback name – unique-ish but deterministic.
            var fallbackName = $"UNNAMED_{context.SourceDtoId:N}";

            context.AddWarning(
                $"AvevaTag is empty or whitespace. Assigning fallback name '{fallbackName}' and marking as candidate for MDB limbo.");

            context.RawInputValue = fallbackName;

            if (string.IsNullOrWhiteSpace(context.NormalizedInputValue))
            {
                context.NormalizedInputValue = fallbackName;
            }
        }

        /// <summary>
        /// Computes HasAnyTokens flag based on TokenEngineResult.
        /// Logs a warning when no processable tokens were produced.
        /// </summary>
        private static void ComputeTokenPresence(DispositionContext context)
        {
            var tokens = context.TokenResult.Tokens;

            context.HasAnyTokens =
                tokens != null &&
                tokens.Values.Any(t => t != null && t.IsProcessable);

            if (!context.HasAnyTokens)
            {
                context.AddWarning("TokenEngine produced no processable tokens for this element.");
            }
        }

        #endregion
    }
}
