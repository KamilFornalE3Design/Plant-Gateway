using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.Tokenization;
using SMSgroup.Aveva.Utilities.Tokenization.Stages;

namespace PlantGateway.Domain.Services.Tokenization
{
    /// <summary>
    /// Orchestrator for the tokenization pipeline.
    /// 
    /// Responsibilities:
    /// - Load and cache TokenRegex / Discipline / Entity / Codification maps.
    /// - Build an ordered list of tokenization stages.
    /// - Execute stages into a TokenizationContext.
    /// - Project context into a public TokenEngineResult.
    /// - Support partial execution up to a given TokenizationStageId.
    /// </summary>
    public sealed class TokenEngine : IEngine
    {
        #region === Constructor & fields ===

        // === Dependent services (loaded once, maps cached) ===
        private readonly IDisciplineHierarchyTokenMapService _disciplineHierarchyTokenMapService;
        private readonly ITokenRegexMapService _tokenRegexMapService;
        private readonly IDisciplineMapService _disciplineMapService;
        private readonly IEntityMapService _entityMapService;
        private readonly ICodificationMapService _codificationMapService;

        // === Cached maps ===
        private IDictionary<string, TokenRegexDTO> _tokenRegexMap = default!;
        private DisciplineHierarchyTokenMapDTO _disciplineHierarchyTokenMap = default!;
        private DisciplineMapDTO _disciplineMap = default!;
        private EntityMapDTO _entityMap = default!;
        private CodificationMapDTO _codificationMap = default!;

        // === Cached lookup ===
        private HashSet<string> _excludedKeys = new(StringComparer.OrdinalIgnoreCase);

        // === Pipeline ===
        private IReadOnlyList<ITokenizationStage> _pipeline = Array.Empty<ITokenizationStage>();

        // === Lock for thread safety ===
        private readonly object _syncRoot = new();
        private bool _initialized;

        public TokenEngine(
            IDisciplineHierarchyTokenMapService disciplineHierarchyTokenMapService,
            ITokenRegexMapService tokenRegexMapService,
            IDisciplineMapService disciplineMapService,
            IEntityMapService entityMapService,
            ICodificationMapService codificationMapService)
        {
            _disciplineHierarchyTokenMapService = disciplineHierarchyTokenMapService ?? throw new ArgumentNullException(nameof(disciplineHierarchyTokenMapService));
            _tokenRegexMapService = tokenRegexMapService ?? throw new ArgumentNullException(nameof(tokenRegexMapService));
            _disciplineMapService = disciplineMapService ?? throw new ArgumentNullException(nameof(disciplineMapService));
            _entityMapService = entityMapService ?? throw new ArgumentNullException(nameof(entityMapService));
            _codificationMapService = codificationMapService ?? throw new ArgumentNullException(nameof(codificationMapService));

            // Initialize immediately upon creation
            Init();
        }

        #endregion

        #region === Initialization & map loading ===

        /// <summary>
        /// Initializes or reloads all dependent maps from their respective services
        /// and rebuilds the internal tokenization pipeline.
        /// </summary>
        public void Init()
        {
            lock (_syncRoot)
            {
                // 1) Load maps
                _tokenRegexMap = _tokenRegexMapService.GetMap().TokenRegex;
                _disciplineHierarchyTokenMap = _disciplineHierarchyTokenMapService.GetMap();
                _disciplineMap = _disciplineMapService.GetMap();
                _entityMap = _entityMapService.GetMap();
                _codificationMap = _codificationMapService.GetMap();

                // 2) Build cached exclusion keys (used by fallback / exception handling)
                _excludedKeys = BuildExclusionSet();

                // 3) Build pipeline stages
                BuildPipeline();

                _initialized = true;
            }
        }

        /// <summary>
        /// Forces reloading of all map files from disk through their services.
        /// Use this when configuration JSON files are updated at runtime.
        /// </summary>
        public void ReloadMaps()
        {
            Init();
        }

        private void EnsureInitialized()
        {
            if (!_initialized ||
                _tokenRegexMap == null ||
                _disciplineHierarchyTokenMap == null ||
                _disciplineMap == null ||
                _entityMap == null ||
                _codificationMap == null)
            {
                throw new InvalidOperationException(
                    "❌ TokenEngine is not initialized. Call Init() before performing token operations.");
            }
        }

        #endregion

        #region === Pipeline construction ===

        /// <summary>
        /// Builds the ordered list of tokenization stages.
        /// 
        /// NOTE:
        /// - The concrete stage types live in the Tokenization/Stages folder.
        /// - Constructor parameters are deliberately simple: only maps and lookups,
        ///   no services, to keep stages easy to test.
        /// </summary>
        private void BuildPipeline()
        {
            var stages = new List<ITokenizationStage>
            {
                // 10: Normalize input, split into parts, basic sanity checks
                new PreProcessingStage(),

                // 20: Codification-first structural detection (Plant / Unit / Section / Equipment)
                new StructuralCodificationStage(_codificationMap),

                // 30: Regex-based base fallback (including PlantSection-level exceptions
                //      like BUILDINGS, WALKWAYS, MANDUMMY, LIFTCAR defined in TokenRegexMap)
                new RegexBaseFallbackStage((IReadOnlyDictionary<string, TokenRegexDTO>)_tokenRegexMap, _excludedKeys),

                // 40: Suffix recognition (Component, Discipline, Entity, TagComposite, TagIncremental, etc.)
                new SuffixRecognitionStage(
                    (IReadOnlyDictionary<string, TokenRegexDTO>)_tokenRegexMap,
                    _disciplineMap,
                    _entityMap,
                    _disciplineHierarchyTokenMap),

                // 50: Codification validation (parent/child consistency, missing codifications)
                new CodificationValidationStage(_codificationMap),

                // 60: Scoring (per-token + total; codification hits, regex fallback, exceptions)
                new ScoringStage(),

                // 70: Final shaping (excluded tokens, ordering, consistency check)
                new PostProcessingStage((IReadOnlyDictionary < string, TokenRegexDTO >) _tokenRegexMap)
            };

            _pipeline = stages;
        }

        #endregion

        #region === Public Execute API ===

        /// <summary>
        /// Executes the full tokenization pipeline (all stages up to PostProcessing).
        /// This is the default used by Parser/Validator/Planner when they want a
        /// complete tokenization for downstream processing.
        /// </summary>
        public TokenEngineResult Execute(ProjectStructureDTO dto)
        {
            return Execute(dto, TokenizationStageId.PostProcessing);
        }

        /// <summary>
        /// Executes the tokenization pipeline up to and including the specified stage.
        /// 
        /// This allows Parser-only, Parser+Codification, or other partial runs to
        /// still obtain a <see cref="TokenEngineResult"/> with messages, warnings,
        /// and any tokens recognized so far.
        /// </summary>
        /// <param name="dto">Source DTO (e.g., ProjectStructureDTO) to tokenize.</param>
        /// <param name="lastStage">
        /// The last internal stage to execute (PreProcessing, StructuralCodification, etc.).
        /// </param>
        public TokenEngineResult Execute(ProjectStructureDTO dto, TokenizationStageId lastStage)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            EnsureInitialized();

            // Prepare internal context
            var ctx = new TokenizationContext
            {
                SourceDtoId = dto.Id,
                RawInput = dto.AvevaTag ?? string.Empty
            };

            // Run stages in order until requested last stage
            foreach (var stage in _pipeline)
            {
                if (stage.Id > lastStage)
                    break;

                stage.Execute(ctx);
                ctx.MarkStageExecuted(stage.Id);
            }

            // Project internal state to public result
            var result = ctx.ToResult();

            return result;
        }

        // NEW: raw AvevaTag API (DTO-free)
        public TokenEngineResult Execute(
            string avevaTag,
            Guid? sourceId = null,
            TokenizationStageId lastStage = TokenizationStageId.PostProcessing)
        {
            EnsureInitialized();

            var ctx = new TokenizationContext
            {
                SourceDtoId = sourceId ?? Guid.Empty,
                RawInput = avevaTag ?? string.Empty
            };

            return ExecuteInternal(ctx, lastStage);
        }

        // Shared internal execution helper
        private TokenEngineResult ExecuteInternal(
            TokenizationContext ctx,
            TokenizationStageId lastStage)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));

            // Run stages in order until requested last stage
            foreach (var stage in _pipeline)
            {
                if (stage.Id > lastStage)
                    break;

                stage.Execute(ctx);
                ctx.MarkStageExecuted(stage.Id);
            }

            return ctx.ToResult();
        }

        #endregion

        #region === Exclusion set (shared helper) ===

        /// <summary>
        /// Builds a set of keys that should be excluded from certain
        /// exception / fallback scans (e.g., core codification tokens,
        /// discipline and entity codes, internal/system entries).
        /// 
        /// This is the same logic as in the legacy implementation, kept
        /// here so stages can rely on _excludedKeys for consistent behavior.
        /// </summary>
        private HashSet<string> BuildExclusionSet()
        {
            var exclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1️⃣ Add codification keys (Plant / Unit / Section / Equipment)
            foreach (var key in _codificationMap.Codifications.Keys)
                exclusions.Add(key);

            // 2️⃣ Add discipline codes (ME, EL, CI, etc.)
            foreach (var key in _disciplineMap.Disciplines.Keys)
                exclusions.Add(key);

            // 3️⃣ Add entity types (SDE, NOZ, DATUM, etc.)
            foreach (var key in _entityMap.Entities.Keys)
                exclusions.Add(key);

            // 4️⃣ Add special/internal TokenRegex definitions to ignore
            foreach (var kv in _tokenRegexMap)
            {
                var key = kv.Key;
                var dto = kv.Value;

                // Skip null or invalid patterns
                if (dto == null || string.IsNullOrWhiteSpace(key))
                    continue;

                // Skip core tokens handled by Codification
                if (key.Equals("Plant", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("PlantUnit", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("PlantSection", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Equipment", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Exclude special tokens and meta definitions
                if (key.StartsWith("TagBase", StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith("TagComposite", StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith("Project", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("Placeholder", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("System", StringComparison.OrdinalIgnoreCase))
                {
                    exclusions.Add(key);
                }
            }

            return exclusions;
        }

        #endregion
    }
}
