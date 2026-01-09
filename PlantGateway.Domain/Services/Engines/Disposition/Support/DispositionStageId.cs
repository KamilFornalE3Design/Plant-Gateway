using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Domain.Services.Engines.Disposition
{
    /// <summary>
    /// Identifiers for all stages in the DispositionEngine pipeline.
    /// </summary>
    public enum DispositionStageId
    {
        /// <summary>
        /// Stage 10: basic input analysis and fallback handling.
        /// </summary>
        PreProcessing = 10,

        /// <summary>
        /// Stage 20: snapshot of structural/functional tokens into flags.
        /// </summary>
        TokenSnapshot = 20,

        /// <summary>
        /// Stage 30: evaluate eligibility for Final / DB Limbo.
        /// </summary>
        QualityAssessment = 30,

        /// <summary>
        /// Stage 40: map eligibility flags to a concrete quality bucket.
        /// </summary>
        BucketAssignment = 40,

        /// <summary>
        /// Stage 50: resolve route and routing hints (server / MDB).
        /// </summary>
        RouteResolution = 50,

        /// <summary>
        /// Stage 60: scoring / final consistency checks.
        /// </summary>
        Scoring = 60
    }
}
