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
    /// Stage 30:
    /// - Evaluates data quality and sets eligibility flags:
    ///   - IsFinalImportEligible
    ///   - IsDbLimboEligible
    ///
    /// This stage does NOT choose the final bucket – it prepares the
    /// quality assessment that DispositionBucketAssignmentStage will map
    /// to FinalImport / DbLimbo / MdbLimbo.
    /// </summary>
    internal sealed class DispositionQualityAssessmentStage : IDispositionStage
    {
        public DispositionStageId Id => DispositionStageId.QualityAssessment;

        public string Name => nameof(DispositionQualityAssessmentStage);

        public void Execute(DispositionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            ValidatePreconditions(context);

            EvaluateFinalImportEligibility(context);
            EvaluateDbLimboEligibility(context);

            LogQualitySummary(context);

            // We can set a preliminary IsValid here:
            // "valid" as long as no hard errors were recorded.
            if (context.Error.Count == 0)
            {
                context.IsValid = true;
            }
        }

        #region === Private helpers ===

        private static void ValidatePreconditions(DispositionContext context)
        {
            if (context.TokenResult == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(DispositionQualityAssessmentStage)} requires a non-null {nameof(TokenEngineResult)} in the context.");
            }

            // If TokenEngine produced no tokens, we already logged a warning in PreProcessing.
            // Here we just acknowledge that quality will be low.
        }

        /// <summary>
        /// Determines whether the element is eligible for final import
        /// based on structural and functional completeness.
        /// </summary>
        private static void EvaluateFinalImportEligibility(DispositionContext context)
        {
            // Structural requirements:
            // Plant + PlantUnit + PlantSection +
            // Component + (Equipment or EquipmentReplacedByComponent)
            bool hasFullStructure =
                context.HasPlant &&
                context.HasPlantUnit &&
                context.HasPlantSection &&
                context.HasComponent &&
                (context.HasEquipment || context.EquipmentReplacedByComponent);

            // Functional requirements:
            // Effective discipline + effective entity.
            bool hasFunctional =
                context.HasEffectiveDiscipline &&
                context.HasEffectiveEntity;

            // Additional guards:
            // - Tag should not be originally empty (even if we assigned a fallback name).
            // - There should be at least some usable tokens.
            bool additionalGuards =
                !context.IsTagEmptyOrWhitespace &&
                context.HasAnyTokens;

            context.IsFinalImportEligible = hasFullStructure && hasFunctional && additionalGuards;

            if (!hasFullStructure)
            {
                var missingParts = new List<string>();

                if (!context.HasPlant) missingParts.Add("Plant");
                if (!context.HasPlantUnit) missingParts.Add("PlantUnit");
                if (!context.HasPlantSection) missingParts.Add("PlantSection");
                if (!context.HasComponent) missingParts.Add("Component");

                if (!context.HasEquipment && !context.EquipmentReplacedByComponent)
                {
                    missingParts.Add("Equipment (or Component replacing Equipment)");
                }

                context.AddMessage(
                    "Final import structural check: missing parts -> " +
                    (missingParts.Count > 0 ? string.Join(", ", missingParts) : "none"));
            }

            if (!hasFunctional)
            {
                var missing = new List<string>();
                if (!context.HasEffectiveDiscipline) missing.Add("Discipline");
                if (!context.HasEffectiveEntity) missing.Add("Entity");

                context.AddMessage(
                    "Final import functional check: missing -> " +
                    (missing.Count > 0 ? string.Join(", ", missing) : "none"));
            }

            if (context.IsTagEmptyOrWhitespace)
            {
                context.AddMessage(
                    "Final import guard: AvevaTag was originally empty/whitespace, " +
                    "so this element cannot be eligible for direct final import.");
            }

            if (!context.HasAnyTokens)
            {
                context.AddMessage(
                    "Final import guard: TokenEngine produced no processable tokens, " +
                    "so this element cannot be eligible for direct final import.");
            }
        }

        /// <summary>
        /// Determines whether the element is eligible for DB limbo.
        /// 
        /// Rule: DB Limbo for components that have AvevaTag with proper:
        /// - Plant
        /// - PlantUnit
        /// - Entity
        /// and are not eligible for final import.
        /// </summary>
        private static void EvaluateDbLimboEligibility(DispositionContext context)
        {
            // Basic requirement: not already eligible for final import.
            if (context.IsFinalImportEligible)
            {
                context.IsDbLimboEligible = false;
                return;
            }

            // Structural minimum for DB limbo:
            // Plant + PlantUnit + Component
            bool hasMinStructure =
                context.HasPlant &&
                context.HasPlantUnit &&
                context.HasComponent;

            // Functional requirement: effective entity.
            bool hasEntity = context.HasEffectiveEntity;

            context.IsDbLimboEligible = hasMinStructure && hasEntity;

            if (!hasMinStructure)
            {
                var missing = new List<string>();
                if (!context.HasPlant) missing.Add("Plant");
                if (!context.HasPlantUnit) missing.Add("PlantUnit");
                if (!context.HasComponent) missing.Add("Component");

                context.AddMessage(
                    "DB limbo structural check: missing parts -> " +
                    (missing.Count > 0 ? string.Join(", ", missing) : "none"));
            }

            if (!hasEntity)
            {
                context.AddMessage("DB limbo functional check: missing Entity.");
            }
        }

        /// <summary>
        /// Logs a compact summary of the quality assessment.
        /// </summary>
        private static void LogQualitySummary(DispositionContext context)
        {
            var parts = new List<string>
            {
                $"FinalEligible={BoolToFlag(context.IsFinalImportEligible)}",
                $"DbLimboEligible={BoolToFlag(context.IsDbLimboEligible)}",
                $"TagEmpty={BoolToFlag(context.IsTagEmptyOrWhitespace)}",
                $"HasTokens={BoolToFlag(context.HasAnyTokens)}",
                $"HasPlant={BoolToFlag(context.HasPlant)}",
                $"HasUnit={BoolToFlag(context.HasPlantUnit)}",
                $"HasSection={BoolToFlag(context.HasPlantSection)}",
                $"HasEquip={BoolToFlag(context.HasEquipment)}",
                $"HasComp={BoolToFlag(context.HasComponent)}",
                $"EffDisc={BoolToFlag(context.HasEffectiveDiscipline)}",
                $"EffEnt={BoolToFlag(context.HasEffectiveEntity)}"
            };

            context.AddMessage("Disposition quality assessment: " + string.Join(", ", parts));
        }

        private static string BoolToFlag(bool value) => value ? "Y" : "N";

        #endregion
    }
}
