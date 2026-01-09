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
    /// Stage 40:
    /// - Maps quality assessment flags to a concrete disposition bucket:
    ///   FinalImport / DbLimbo / MdbLimbo.
    ///
    /// This stage does not handle routing (server / MDB) or scoring.
    /// Those are handled by the subsequent RouteResolution and Scoring stages.
    /// </summary>
    internal sealed class DispositionBucketAssignmentStage : IDispositionStage
    {
        public DispositionStageId Id => DispositionStageId.BucketAssignment;

        public string Name => nameof(DispositionBucketAssignmentStage);

        public void Execute(DispositionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            AssignBucket(context);
            FinalizeValidity(context);
            LogBucketSummary(context);
        }

        #region === Private helpers ===

        private static void AssignBucket(DispositionContext context)
        {
            // Priority:
            // 1) Final import if fully eligible.
            // 2) DB limbo if minimum quality is met.
            // 3) MDB limbo for everything else.

            if (context.IsFinalImportEligible)
            {
                context.QualityBucket = DispositionQualityBucket.FinalImport;
                context.AddMessage("Disposition bucket assignment: FinalImport (meets full structural and functional criteria).");
                return;
            }

            if (context.IsDbLimboEligible)
            {
                context.QualityBucket = DispositionQualityBucket.DbLimbo;
                context.AddMessage("Disposition bucket assignment: DbLimbo (has Plant, PlantUnit, Component and effective Entity, but not enough for final import).");
                return;
            }

            // Fallback: MDB limbo – for empty tags or generally low-quality input.
            context.QualityBucket = DispositionQualityBucket.MdbLimbo;

            if (context.IsTagEmptyOrWhitespace)
            {
                context.AddMessage("Disposition bucket assignment: MdbLimbo (AvevaTag originally empty/whitespace).");
            }
            else if (!context.HasAnyTokens)
            {
                context.AddMessage("Disposition bucket assignment: MdbLimbo (TokenEngine produced no processable tokens).");
            }
            else
            {
                context.AddMessage("Disposition bucket assignment: MdbLimbo (does not meet criteria for FinalImport or DbLimbo).");
            }
        }

        /// <summary>
        /// Sets IsValid according to the bucket and existing errors.
        /// </summary>
        private static void FinalizeValidity(DispositionContext context)
        {
            // If an earlier stage signalled hard errors, respect that.
            if (context.Error.Count > 0)
            {
                context.IsValid = false;
                return;
            }

            // If we assigned any concrete bucket, treat the disposition as logically valid.
            context.IsValid = context.QualityBucket != DispositionQualityBucket.Unknown;
        }

        /// <summary>
        /// Logs a compact summary of the chosen bucket.
        /// </summary>
        private static void LogBucketSummary(DispositionContext context)
        {
            var bucket = context.QualityBucket.ToString();

            context.AddMessage($"Disposition bucket summary: Bucket={bucket}, " +
                               $"FinalEligible={BoolToFlag(context.IsFinalImportEligible)}, " +
                               $"DbLimboEligible={BoolToFlag(context.IsDbLimboEligible)}, " +
                               $"TagEmpty={BoolToFlag(context.IsTagEmptyOrWhitespace)}, " +
                               $"HasTokens={BoolToFlag(context.HasAnyTokens)}");
        }

        private static string BoolToFlag(bool value) => value ? "Y" : "N";

        #endregion
    }
}
