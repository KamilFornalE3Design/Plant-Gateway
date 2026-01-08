using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using System;
using System.Collections.Generic;

namespace PlantGateway.Application.Pipelines.Results.Execution
{
    /// <summary>
    /// Represents the outcome of parsing an input target before validation and planning.
    /// Shared across all pipelines operating on <see cref="IPlantGatewayDTO"/> types.
    /// </summary>
    public sealed class ParserResult
    {
        public List<IStepResult> OrderedSteps { get; } = new List<IStepResult>();
        public DataQualityScoreModel Quality { get; set; } = new DataQualityScoreModel();
        public double DataQualityScore { get; set; }



        /// <summary>
        /// Indicates whether the parser successfully recognized and read the input structure.
        /// </summary>
        public bool IsValid =>
            Error.Count == 0 &&
            Warnings.Count == 0;

        /// <summary>
        /// Version/Revision info detected from the input, if any.
        /// I make first steps with versioning, so this is not particulary fixed info and more like overall guidance for next steps.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Indicates that the parser fully completed all internal checks and produced
        /// consistent data usable for further processing.
        /// </summary>
        public bool IsSuccess =>
            IsValid;

        /// <summary>
        /// High-level category of the parsed input (e.g. TOP, ProjectStructure, Geometry).
        /// Used for routing the next pipeline phases.
        /// </summary>
        public DetectedInputSchema DetectedInputSchema { get; set; } = DetectedInputSchema.Unknown;

        /// <summary>
        /// Source system reported in the file header (e.g. CreoParametric, Inventor).
        /// </summary>
        public SourceSystem SourceSystem { get; set; } = SourceSystem.Unknown;

        /// <summary>
        /// Human-readable summary of what the parser found.
        /// Example: "Detected new TakeOverPoint layout with extended AVEVA headers."
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// DTO type this input is expected to map to, e.g. typeof(TakeOverPointDTO).
        /// </summary>
        public IPlantGatewayDTO TargetDtoType { get; set; }

        /// <summary>
        /// Flexible hint storage for parser-specific details.
        /// Keys should be clear and namespaced when needed.
        /// Example:
        ///   { "HeaderVersion", "New" },
        ///   { "HasAvevaTag", true },
        ///   { "RawHeaders", new List&lt;string&gt;() }
        /// </summary>
        public Dictionary<string, object> ParserHints { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // List of unexpected headers found during parsing. It is more placeholder, but useful for diagnostics.
        public List<string> UnexpectedHeaders { get; set; } = new List<string>();

        // List of messages produced during parsing for logging or user feedback.
        public List<string> Message { get; set; } = new List<string>();
        /// <summary>
        /// Non-fatal issues or parser diagnostics.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
        /// <summary>
        /// Fatal errors that prevented successful parsing.
        /// 
        public List<string> Error { get; set; } = new List<string>();

        public override string ToString()
            => $"[{DetectedInputSchema}] {(IsSuccess ? "✅ Success" : "⚠ Incomplete")} – {Summary}";


        // 1) Keep this generic score as "overall parser quality"
        // public double DataQualityScore { get; set; }

        // 2) Optional: tokenization-specific score (0–100)
        public int TokenizationScore { get; set; }

        // 3) Optional: tokenization-specific messages (aggregated, not per DTO)
        public List<string> TokenizationMessages { get; set; } = new List<string>();

        // 4) Store a representative sample (or aggregated) TokenEngineResult if you want
        public TokenEngineResult SampleTokenization { get; set; }
    }

    /// <summary>
    /// Enumerates known categories of input schemas detected by parsers.
    /// </summary>
    public enum DetectedInputSchema
    {
        Unknown = 0,
        TOP = 1,
        ProjectStructure = 2,
        Geometry = 3,
        Configuration = 4
    }
}
