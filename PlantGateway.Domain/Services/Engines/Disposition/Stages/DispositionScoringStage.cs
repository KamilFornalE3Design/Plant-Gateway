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
    /// Stage 60:
    /// - Performs final consistency checks between eligibility flags
    ///   and the chosen quality bucket.
    /// - Marks the disposition as consistency-checked.
    /// - Logs a simple quality-level message (placeholder for future scoring).
    /// 
    /// This stage does NOT change the QualityBucket or Route; it only
    /// validates that they make sense with the rest of the context.
    /// </summary>
    internal sealed class DispositionScoringStage : IDispositionStage
    {
        public DispositionStageId Id => DispositionStageId.Scoring;

        public string Name => nameof(DispositionScoringStage);

        public void Execute(DispositionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            PerformConsistencyChecks(context);
            LogScoringSummary(context);

            context.IsConsistencyChecked = true;
        }

        #region === Private helpers ===

        /// <summary>
        /// Ensures that the assigned QualityBucket is coherent with
        /// the eligibility flags. Adds errors and adjusts IsValid
        /// if contradictions are detected.
        /// </summary>
        private static void PerformConsistencyChecks(DispositionContext context)
        {
            switch (context.QualityBucket)
            {
                case DispositionQualityBucket.FinalImport:
                    if (!context.IsFinalImportEligible)
                    {
                        context.AddError(
                            "Consistency check: Bucket=FinalImport but IsFinalImportEligible is false. " +
                            "This indicates a mismatch between quality assessment and bucket assignment.");
                        context.IsValid = false;
                    }
                    break;

                case DispositionQualityBucket.DbLimbo:
                    if (!context.IsDbLimboEligible)
                    {
                        context.AddError(
                            "Consistency check: Bucket=DbLimbo but IsDbLimboEligible is false. " +
                            "This indicates a mismatch between quality assessment and bucket assignment.");
                        context.IsValid = false;
                    }
                    break;

                case DispositionQualityBucket.MdbLimbo:
                    // MDB limbo is the catch-all; however, if any eligibility flag
                    // says we *could* do better, it's suspicious.
                    if (context.IsFinalImportEligible || context.IsDbLimboEligible)
                    {
                        context.AddError(
                            "Consistency check: Bucket=MdbLimbo but eligibility flags indicate " +
                            "FinalImport or DbLimbo is possible. This should not normally happen.");
                        context.IsValid = false;
                    }
                    break;

                case DispositionQualityBucket.Unknown:
                default:
                    // Unknown bucket is always inconsistent as an end state.
                    context.AddError(
                        "Consistency check: QualityBucket is Unknown at the end of the pipeline. " +
                        "Disposition is not valid.");
                    context.IsValid = false;
                    break;
            }
        }

        /// <summary>
        /// Logs a simple quality-level summary based on the bucket.
        /// This acts as a placeholder for future numeric scoring.
        /// </summary>
        private static void LogScoringSummary(DispositionContext context)
        {
            string qualityLevel;

            switch (context.QualityBucket)
            {
                case DispositionQualityBucket.FinalImport:
                    qualityLevel = "High";
                    break;

                case DispositionQualityBucket.DbLimbo:
                    qualityLevel = "Medium";
                    break;

                case DispositionQualityBucket.MdbLimbo:
                    qualityLevel = "Low";
                    break;

                case DispositionQualityBucket.Unknown:
                default:
                    qualityLevel = "Undefined";
                    break;
            }

            context.AddMessage(
                $"Disposition scoring summary: QualityLevel={qualityLevel}, " +
                $"Bucket={context.QualityBucket}, " +
                $"IsValid={BoolToFlag(context.IsValid)}, " +
                $"IsConsistencyChecked={BoolToFlag(context.IsConsistencyChecked)}");
        }

        private static string BoolToFlag(bool value) => value ? "Y" : "N";

        #endregion
    }
}
