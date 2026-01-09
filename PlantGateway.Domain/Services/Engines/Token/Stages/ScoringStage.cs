using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Models.Tokenization;

namespace PlantGateway.Domain.Services.Engines.Token
{
    /// <summary>
    /// Normalizes raw token scores into a 0–100 quality score.
    /// Uses TokenScores / TotalScore gathered by previous stages
    /// (codification, regex fallback, suffix recognition, etc.).
    /// </summary>
    public sealed class ScoringStage : ITokenizationStage
    {
        public TokenizationStageId Id => TokenizationStageId.Scoring;

        public void Execute(TokenizationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            // -----------------------------------------------------------------
            // 1) Determine raw score
            // -----------------------------------------------------------------
            double rawScore = context.TotalScore;

            if (context.TokenScores != null && context.TokenScores.Count > 0)
            {
                var sum = context.TokenScores.Values.Sum();
                // If TotalScore got out of sync, prefer the sum of per-token scores
                if (Math.Abs(sum - rawScore) > 0.001)
                    rawScore = sum;
            }

            // If we really have nothing, just bail with 0
            if ((context.TokenScores == null || context.TokenScores.Count == 0) &&
                context.Tokens.Count == 0)
            {
                context.Score0To100 = 0;
                context.AddWarning("⚠️ Scoring: no tokens and no scores available; quality set to 0.");
                return;
            }

            // -----------------------------------------------------------------
            // 2) Normalize raw score to 0–100
            // -----------------------------------------------------------------
            // Heuristic range – tune later if needed
            const double minRaw = -40.0;
            const double maxRaw = 120.0;

            var clamped = Math.Min(Math.Max(rawScore, minRaw), maxRaw);
            var normalized = (clamped - minRaw) / (maxRaw - minRaw) * 100.0;

            var score = (int)Math.Round(normalized);
            if (score < 0) score = 0;
            if (score > 100) score = 100;

            // -----------------------------------------------------------------
            // 3) Penalize for warnings / errors
            // -----------------------------------------------------------------
            if (context.Error.Count > 0)
            {
                // Hard issues → cap at 20
                score = Math.Min(score, 20);
            }
            else if (context.Warning.Count > 0)
            {
                // Soft issues → cap at 80
                score = Math.Min(score, 80);
            }

            // -----------------------------------------------------------------
            // 4) Store and log
            // -----------------------------------------------------------------
            context.Score0To100 = score;
            context.AddMessage($"📊 Scoring: tokenization quality score = {score}/100 (raw={rawScore}).");
        }
    }
}
