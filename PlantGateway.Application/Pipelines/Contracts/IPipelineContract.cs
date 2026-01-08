using PlantGateway.Application.Pipelines.Results.Execution;
using PlantGateway.Application.Abstractions.Configuration.Providers;
using System.Collections.Generic;

namespace PlantGateway.Application.Pipelines.Contracts
{
    /// <summary>
    /// Non-generic base contract used by format-level pipeline stages
    /// (Parser, Validator, etc.). Provides unified access to input/output
    /// metadata, context dictionary, and shared results.
    /// </summary>
    public interface IPipelineContract
    {
        // ───────────── Input / Output ─────────────

        /// <summary>
        /// Describes the source of the input (file path, format, etc.).
        /// </summary>
        InputTarget Input { get; set; }

        /// <summary>
        /// Describes where results should be written (output file, database, etc.).
        /// </summary>
        OutputTarget Output { get; set; }

        ParserResult ParserResult { get; set; }
        ValidatorResult ValidatorResult { get; set; }
        PlannerResult PlannerResult { get; set; }

        // I don't habe the concept how to manage generic results i this pipeline interface
        //WalkerResult<TDto> WalkerResult { get; set; } 
        //ProcessorResult<TDto> ProcessorResult { get; set; } 
        //WriterResult<TDto> WriterResult { get; set; }

        // ───────────── Shared Context ─────────────

        /// <summary>
        /// Global configuration provider accessible to all pipeline stages.
        /// </summary>
        IConfigProvider Config { get; }

        // ───────────── Typed Bridging ─────────────

        /// <summary>
        /// Gets or sets the current untyped data collection
        /// (used by non-generic stages that cannot access TDto directly).
        /// Typically maps to the typed Items list in the generic contract.
        /// </summary>
        object UntypedData { get; set; }
    }
}
