using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Utilities.Engines.Disposition.Stages;
using SMSgroup.Aveva.Config.Models.Disposition;
using PlantGateway.Domain.Services.Engines.Abstractions;

namespace SMSgroup.Aveva.Utilities.Engines.Disposition
{
    /// <summary>
    /// Orchestrator for the disposition pipeline.
    ///
    /// Responsibilities:
    /// - Build an ordered list of Distribution*Stage instances.
    /// - Execute stages into a DispositionContext.
    /// - Project context into a public DispositionResult.
    ///
    /// This engine is intended to run late in the processing pipeline,
    /// when tokenization / discipline / entity have already been resolved.
    /// </summary>
    public sealed class DispositionEngine : IEngine
    {
        // === Pipeline ===
        private IReadOnlyList<IDispositionStage> _pipeline = Array.Empty<IDispositionStage>();

        // === Lock for thread safety ===
        private readonly object _syncRoot = new();
        private bool _initialized;

        public DispositionEngine()
        {
            Init();
        }

        #region === Initialization & pipeline ===

        /// <summary>
        /// Initializes the engine and rebuilds the internal disposition pipeline.
        /// </summary>
        public void Init()
        {
            lock (_syncRoot)
            {
                BuildPipeline();
                _initialized = true;
            }
        }

        /// <summary>
        /// For future-proofing: if disposition rules become config-driven,
        /// this method can be used to reload them and rebuild the pipeline.
        /// </summary>
        public void ReloadMaps()
        {
            // No external maps yet, but we keep the same interface as TokenEngine.
            Init();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            Init();
        }

        /// <summary>
        /// Builds the ordered list of Disposition stages.
        /// All stages are named Disposition*Stage as per convention.
        /// </summary>
        private void BuildPipeline()
        {
            var stages = new List<IDispositionStage>
            {
                // 10: Basic input analysis (empty tag, alternative name, etc.)
                new DispositionPreProcessingStage(),

                // 20: Snapshot of token / engine results into internal flags.
                new DispositionTokenSnapshotStage(),

                // 30: Quality evaluation (Final eligible / DB limbo / MDB limbo).
                new DispositionQualityAssessmentStage(),

                // 40: Turn quality assessment into a concrete bucket.
                new DispositionBucketAssignmentStage(),

                // 50: Resolve routing hints (server / MDB / hierarchy route).
                new DispositionRouteResolutionStage(),

                // 60: Optional scoring for analytics and diagnostics.
                new DispositionScoringStage()
            };

            _pipeline = stages;
        }

        #endregion

        #region === Public Execute API ===

        /// <summary>
        /// Executes the full disposition pipeline for a single DTO.
        ///
        /// Assumes that TokenEngine (and any Discipline/Entity engines) have
        /// already been executed and their results are available.
        /// </summary>
        /// <param name="dto">Source DTO (e.g. ProjectStructureDTO) to classify.</param>
        /// <param name="tokenResult">
        /// Tokenization result to base quality / limbo decisions on.
        /// </param>
        /// <returns>Final <see cref="DispositionResult"/> for this DTO.</returns>
        public DispositionResult Execute(ProjectStructureDTO dto, TokenEngineResult tokenResult)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (tokenResult == null)
                throw new ArgumentNullException(nameof(tokenResult));

            EnsureInitialized();

            // Prepare internal context – you’ll flesh this type out next.
            var ctx = new DispositionContext
            {
                SourceDtoId = dto.Id,
                RawInputValue = tokenResult.RawInputValue ?? string.Empty,
                TokenResult = tokenResult,
                // Later: DisciplineResult, EntityResult, additional flags, etc.
            };

            // Run all stages in order
            foreach (var stage in _pipeline)
            {
                stage.Execute(ctx);
            }

            // Project internal state to public result
            var result = ctx.ToResult();

            return result;
        }

        /// <summary>
        /// Convenience API used by processor strategy:
        /// resolves TokenEngineResult from dto.EngineResults and runs disposition.
        /// Returns null if no TokenEngineResult is available (so AddIfNotNull can skip).
        /// </summary>
        public DispositionResult? Process(ProjectStructureDTO dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var tokenResult = dto.EngineResults?
                .OfType<TokenEngineResult>()
                .LastOrDefault();

            if (tokenResult == null)
            {
                // No token result → we can't classify.
                // Leave diagnostics to higher level; AddIfNotNull will ignore null.
                return null;
            }

            return Execute(dto, tokenResult);
        }

        #endregion
    }
}
