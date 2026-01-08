using PlantGateway.Application.Pipelines.Results.Engines;
using PlantGateway.Application.Pipelines.Results.Execution;
using PlantGateway.Application.Abstractions.Configuration.Providers;
using PlantGateway.Core.Config.Models.PlannerBlocks;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Contracts
{
    /// <summary>
    /// Generic container representing the entire state of a pipeline execution
    /// for a specific DTO type (e.g. TakeOverPointDTO, ProjectStructureDTO).
    /// </summary>
    /// <typeparam name="TDto">DTO type handled by this pipeline.</typeparam>
    public sealed class PipelineContract<TDto> : IPipelineContract
    {
        // ─────────────────────────────
        // ──────── Target DTO ─────────
        // ─────────────────────────────

        /// <summary>
        /// The DTO type that this pipeline operates on.
        /// Automatically derived from <typeparamref name="TDto"/>.
        /// </summary>
        public Type TargetDtoType => typeof(TDto);

        /// <summary>
        /// The actual data items being processed by the pipeline.
        /// </summary>
        public IList<TDto> Items { get; set; }
        public IEnumerable<IGrouping<int, Guid>> HierarchyGroups { get; set; }

        // ─────────────────────────────
        // ───── Input / Output Targets ─
        // ─────────────────────────────

        /// <summary>
        /// Describes the source of the data (e.g. file path, format, DTO type).
        /// Usually built from CLI ConvertSettings.
        /// </summary>
        public InputTarget Input { get; set; }

        /// <summary>
        /// Describes where results should be written (e.g. output file, database).
        /// </summary>
        public OutputTarget Output { get; set; }

        public ProcessPhase Phase { get; set; }

        // ─────────────────────────────
        // ───── Contract Variables ────
        // ─────────────────────────────

        public CsysWRT CsysWRT { get; set; }
        public CsysReferenceOffset CsysReferenceOffset { get; set; }
        public CsysOption CsysOption { get; set; }

        // ─────────────────────────────
        // ───── Intermediate Results ──
        // ─────────────────────────────

        /// <summary>
        /// Result from the parser phase.
        /// </summary>
        public ParserResult ParserResult { get; set; } = new ParserResult();

        /// <summary>
        /// Result from the validation phase.
        /// </summary>
        public ValidatorResult ValidatorResult { get; set; } = new ValidatorResult();

        /// <summary>
        /// Strategy plan produced by the planner phase.
        /// </summary>
        public PlannerResult PlannerResult { get; set; } = new PlannerResult();
        public WalkerResult<TDto> WalkerResult { get; set; } = new WalkerResult<TDto>();
        public ProcessorResult<TDto> ProcessorResult { get; set; } = new ProcessorResult<TDto>();
        public WriterResult<TDto> WriterResult { get; set; } = new WriterResult<TDto>();


        public Dictionary<Guid, TDto> ItemsLookup { get; set; } // for linking input/dto/output
        public List<PGNode<XElement>> Metadata { get; set; }

        /// <summary>
        /// Consolidated structural hierarchy built from all <see cref="HierarchyEngineResult"/>s.
        /// Produced by <see cref="HierarchyConsolidationHandler"/> and used by writers.
        /// </summary>
        public List<HierarchyNode> ConsolidatedStructure { get; set; } = new List<HierarchyNode>();
        /// <summary>
        /// Final hierarchy tree built after consolidation. 
        /// Contains all AVEVA structural nodes ready for writers.
        /// </summary>
        public HierarchyTree ConsolidatedTree { get; set; }




        // Add this property to bridge untyped ↔ typed
        public object UntypedData
        {
            get => Items;
            set => Items = value is IList<TDto> list ? list : new List<TDto>();
        }


        // ─────────────────────────────
        // ───── Shared Environment ─────
        // ─────────────────────────────

        /// <summary>
        /// Global configuration provider accessible to all pipeline stages.
        /// </summary>
        public IConfigProvider Config { get; }

        // ─────────────────────────────
        // ───── Convenience Flags ─────
        // ─────────────────────────────

        /// <summary>
        /// True if validation succeeded and execution may continue.
        /// </summary>
        public bool IsValid => ValidatorResult?.Success ?? false;

        /// <summary>
        /// True if parser finished without critical errors.
        /// </summary>
        public bool IsParsed => ParserResult?.IsValid ?? false;


        // ─────────────────────────────
        // ─────── Constructors ────────
        // ─────────────────────────────

        public PipelineContract(IEnumerable<TDto> items, IConfigProvider config)
        {
            Items = items?.ToList() ?? new List<TDto>();
            Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // ─────────────────────────────
        // ─────── Helper Methods ──────
        // ─────────────────────────────

        ///// <summary>
        ///// Adds a single engine result to the global collection.
        ///// </summary>
        //public void AddResult(TDto DTO, IEngineResult result)
        //{
        //    if (result != null)
        //        DTO.EngineResults.Add(result);
        //}

        ///// <summary>
        ///// Adds multiple engine results.
        ///// </summary>
        //public void AddResults(IEnumerable<IEngineResult> results)
        //{
        //    if (results != null)
        //        EngineResults.AddRange(results);
        //}

        ///// <summary>
        ///// Retrieves a specific engine result type, if available.
        ///// </summary>
        //public T GetResult<T>() where T : class, IEngineResult
        //{
        //    return EngineResults.OfType<T>().FirstOrDefault();
        //}

        public void AddWarning(string message)
        {
            if (ValidatorResult == null)
                ValidatorResult = new ValidatorResult();

            ValidatorResult.Warnings.Add(message);
        }
    }
}
