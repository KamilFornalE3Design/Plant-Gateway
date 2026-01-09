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
    /// Stage 20:
    /// - Takes a snapshot of structural and functional tokens from TokenEngineResult,
    /// - Fills DispositionContext flags (HasPlant, HasPlantUnit, HasPlantSection, HasEquipment, HasComponent),
    /// - Detects the "Component replaces Equipment" scenario,
    /// - Sets basic HasEffectiveDiscipline / HasEffectiveEntity flags.
    /// 
    /// This stage does NOT decide eligibility or buckets – it only prepares
    /// the input needed by the quality assessment stage.
    /// </summary>
    internal sealed class DispositionTokenSnapshotStage : IDispositionStage
    {
        public DispositionStageId Id => DispositionStageId.TokenSnapshot;

        public string Name => nameof(DispositionTokenSnapshotStage);

        public void Execute(DispositionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            ValidateTokenResult(context);

            var tokens = context.TokenResult.Tokens ?? new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);

            SnapshotStructuralTokens(context, tokens);
            SnapshotFunctionalTokens(context, tokens);
            LogStructuralSummary(context);
        }

        #region === Private helpers ===

        private static void ValidateTokenResult(DispositionContext context)
        {
            if (context.TokenResult == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(DispositionTokenSnapshotStage)} requires a non-null {nameof(TokenEngineResult)} in the context.");
            }
        }

        /// <summary>
        /// Fills structural flags on the context based on processable tokens:
        /// - HasPlant, HasPlantUnit, HasPlantSection, HasEquipment, HasComponent,
        /// - EquipmentReplacedByComponent (Component serving as Equipment).
        /// </summary>
        private static void SnapshotStructuralTokens(DispositionContext context, IDictionary<string, Token> tokens)
        {
            // Plant
            context.HasPlant = TryGetProcessable(tokens, "Plant", out _);

            // PlantUnit
            context.HasPlantUnit = TryGetProcessable(tokens, "PlantUnit", out _);

            // PlantSection
            // Prefer processable token, but if not found, fall back to simple presence
            // via the existing HasPlantSectionToken flag.
            context.HasPlantSection =
                TryGetProcessable(tokens, "PlantSection", out _) ||
                context.TokenResult.HasPlantSectionToken;

            // Equipment / Component
            var hasEquipment = TryGetProcessable(tokens, "Equipment", out var equipmentToken);
            var hasComponent = TryGetProcessable(tokens, "Component", out var componentToken);

            context.HasEquipment = hasEquipment;
            context.HasComponent = hasComponent;

            // Detect "Component replaces Equipment" case:
            // - No processable Equipment token,
            // - Component token is a replacement with ReplacesKey == "Equipment".
            context.EquipmentReplacedByComponent =
                !hasEquipment &&
                hasComponent &&
                componentToken.IsReplacement &&
                string.Equals(componentToken.ReplacesKey, "Equipment", StringComparison.OrdinalIgnoreCase);

            if (context.EquipmentReplacedByComponent)
            {
                context.AddMessage("Component token is replacing Equipment; treating Equipment as present via replacement semantics.");
                context.HasEquipment = true; // logically treat as present for downstream quality checks
            }
        }

        /// <summary>
        /// Fills functional flags:
        /// - HasEffectiveDiscipline,
        /// - HasEffectiveEntity.
        /// 
        /// For now we treat "effective" as "processable token exists".
        /// Later you can enrich this with DisciplineEngine / EntityEngine results.
        /// </summary>
        private static void SnapshotFunctionalTokens(DispositionContext context, IDictionary<string, Token> tokens)
        {
            // Discipline
            var hasDisciplineToken = TryGetProcessable(tokens, "Discipline", out _)
                                     || context.TokenResult.HasDisciplineToken;

            // Entity
            var hasEntityToken = TryGetProcessable(tokens, "Entity", out _)
                                 || context.TokenResult.HasEntityToken;

            context.HasEffectiveDiscipline = hasDisciplineToken;
            context.HasEffectiveEntity = hasEntityToken;

            // In the future, when you attach DisciplineEngineResult / EntityEngineResult
            // to DispositionContext, you can extend this logic to treat inherited/overridden
            // values as "effective", even when no direct token is present.
        }

        /// <summary>
        /// Logs a compact summary of structural tokens for diagnostics.
        /// </summary>
        private static void LogStructuralSummary(DispositionContext context)
        {
            var parts = new List<string>
            {
                $"Plant={BoolToFlag(context.HasPlant)}",
                $"Unit={BoolToFlag(context.HasPlantUnit)}",
                $"Section={BoolToFlag(context.HasPlantSection)}",
                $"Equip={BoolToFlag(context.HasEquipment)}",
                $"Comp={BoolToFlag(context.HasComponent)}",
                $"EquipByComp={BoolToFlag(context.EquipmentReplacedByComponent)}"
            };

            context.AddMessage("Disposition structural snapshot: " + string.Join(", ", parts));
        }

        private static string BoolToFlag(bool value) => value ? "Y" : "N";

        /// <summary>
        /// Helper to check if a processable token exists for a given key.
        /// </summary>
        private static bool TryGetProcessable(IDictionary<string, Token> tokens, string key, out Token token)
        {
            token = null;

            if (tokens == null)
                return false;

            if (!tokens.TryGetValue(key, out var raw) || raw == null)
                return false;

            if (!raw.IsProcessable)
                return false;

            token = raw;
            return true;
        }

        #endregion
    }
}
