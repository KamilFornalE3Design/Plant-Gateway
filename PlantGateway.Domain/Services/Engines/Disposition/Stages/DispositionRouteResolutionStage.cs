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
    /// Stage 50:
    /// - Resolves the logical route for hierarchy placement
    ///   based on the chosen quality bucket.
    /// - Resolves TargetServerKey from Entity.
    /// - Resolves TargetMdbKey from Plant.
    ///
    /// This stage is intentionally kept simple and non-config-driven.
    /// It just exposes the values needed for logging and for later
    /// mapping to real servers / MDBs.
    /// </summary>
    internal sealed class DispositionRouteResolutionStage : IDispositionStage
    {
        public DispositionStageId Id => DispositionStageId.RouteResolution;

        public string Name => nameof(DispositionRouteResolutionStage);

        public void Execute(DispositionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            ValidatePreconditions(context);

            ResolveRouteFromBucket(context);
            ResolveTargetServerFromEntity(context);
            ResolveTargetMdbFromPlant(context);

            LogRouteSummary(context);
        }

        #region === Private helpers ===

        private static void ValidatePreconditions(DispositionContext context)
        {
            if (context.QualityBucket == DispositionQualityBucket.Unknown)
            {
                context.AddWarning(
                    $"{nameof(DispositionRouteResolutionStage)}: QualityBucket is Unknown. " +
                    "Routing will be minimal and may need review.");
            }

            if (context.TokenResult == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(DispositionRouteResolutionStage)} requires a non-null {nameof(TokenEngineResult)} in the context.");
            }
        }

        /// <summary>
        /// Maps the quality bucket to a logical route string
        /// that HierarchyEngine can interpret.
        /// </summary>
        private static void ResolveRouteFromBucket(DispositionContext context)
        {
            switch (context.QualityBucket)
            {
                case DispositionQualityBucket.FinalImport:
                    context.Route = "ProductionHierarchy";
                    break;

                case DispositionQualityBucket.DbLimbo:
                    context.Route = "DbLimboHierarchy";
                    break;

                case DispositionQualityBucket.MdbLimbo:
                    context.Route = "MdbLimboHierarchy";
                    break;

                case DispositionQualityBucket.Unknown:
                default:
                    context.Route = string.Empty;
                    break;
            }

            if (string.IsNullOrWhiteSpace(context.Route))
            {
                context.AddWarning(
                    "Disposition route resolution: no route resolved (QualityBucket is Unknown).");
            }
        }

        /// <summary>
        /// Resolves TargetServerKey from the Entity token.
        /// For now this is a direct passthrough of the Entity value,
        /// so logs clearly show which entity we would base routing on.
        /// </summary>
        private static void ResolveTargetServerFromEntity(DispositionContext context)
        {
            var tokens = context.TokenResult.Tokens ?? new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);

            if (tokens.TryGetValue("Entity", out var entityToken) &&
                entityToken != null &&
                entityToken.IsProcessable &&
                !string.IsNullOrWhiteSpace(entityToken.Value))
            {
                // Minimal, non-config version: just expose the entity value.
                context.TargetServerKey = entityToken.Value;

                context.AddMessage(
                    $"Disposition routing: TargetServerKey resolved from Entity token -> '{context.TargetServerKey}'.");
            }
            else
            {
                context.TargetServerKey = string.Empty;

                if (context.HasEffectiveEntity)
                {
                    // Entity is "effective" but we couldn't find a direct token value
                    // (e.g., inherited). Log this so we can extend logic later.
                    context.AddMessage(
                        "Disposition routing: Effective Entity detected but no direct Entity token value " +
                        "found for TargetServerKey. Routing will need extension when client rules are defined.");
                }
                else
                {
                    context.AddMessage(
                        "Disposition routing: No effective Entity; TargetServerKey remains empty.");
                }
            }
        }

        /// <summary>
        /// Resolves TargetMdbKey from the Plant token.
        /// For now this is a direct passthrough of the Plant value,
        /// so logs clearly show which plant we would base MDB selection on.
        /// </summary>
        private static void ResolveTargetMdbFromPlant(DispositionContext context)
        {
            var tokens = context.TokenResult.Tokens ?? new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);

            if (tokens.TryGetValue("Plant", out var plantToken) &&
                plantToken != null &&
                plantToken.IsProcessable &&
                !string.IsNullOrWhiteSpace(plantToken.Value))
            {
                // Minimal, non-config version: just expose the plant value.
                context.TargetMdbKey = plantToken.Value;

                context.AddMessage(
                    $"Disposition routing: TargetMdbKey resolved from Plant token -> '{context.TargetMdbKey}'.");
            }
            else
            {
                context.TargetMdbKey = string.Empty;

                if (context.HasPlant)
                {
                    context.AddMessage(
                        "Disposition routing: Plant flag is true but no processable Plant token value " +
                        "found for TargetMdbKey. Routing will need extension when client rules are defined.");
                }
                else
                {
                    context.AddMessage(
                        "Disposition routing: No Plant detected; TargetMdbKey remains empty.");
                }
            }
        }

        /// <summary>
        /// Logs a compact summary of routing decisions.
        /// </summary>
        private static void LogRouteSummary(DispositionContext context)
        {
            context.AddMessage(
                "Disposition route summary: " +
                $"Bucket={context.QualityBucket}, " +
                $"Route='{context.Route}', " +
                $"TargetServerKey='{context.TargetServerKey}', " +
                $"TargetMdbKey='{context.TargetMdbKey}'");
        }

        #endregion
    }
}
