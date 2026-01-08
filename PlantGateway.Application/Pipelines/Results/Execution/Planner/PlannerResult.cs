using PlantGateway.Core.Config.Models.PlannerBlocks;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using System;
using System.Collections.Generic;

namespace PlantGateway.Application.Pipelines.Results.Execution
{
    /// <summary>
    /// Immutable value object representing an execution plan
    /// derived from Parser + Validator analysis.
    /// </summary>
    public sealed class PlannerResult
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Detected schema (ProjectStructure, TOP, Unknown…).
        /// </summary>
        public string Schema { get; set; } = string.Empty;

        /// <summary>
        /// Type of DTO this plan targets (e.g. TakeOverPointDTO).
        /// </summary>
        public Type TargetDtoType { get; set; } = typeof(object);

        /// <summary>
        /// A list of planned high-level actions or processing stages
        /// that the pipeline should execute.
        /// </summary>
        public List<string> ExecutionSteps { get; set; } = new List<string>();

        /// <summary>
        /// Fine-grained boolean flags controlling behavior in Walker or Processor.
        /// For example: { "MergePrefixedTags": true, "SkipValidation": false }.
        /// </summary>
        public Dictionary<string, bool> ExecutionFlags { get; set; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Optional parameter bag for additional planner-level configuration.
        /// Example: { "HeaderVersion": "New", "WalkerMode": "Extended" }.
        /// </summary>
        public Dictionary<string, object> ExecutionParameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Warnings or advisories collected during planning.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Human-readable summary of what plan was created.
        /// </summary>
        public string Summary { get; set; } = string.Empty;
        // Positioning options
        public CsysOption CsysOption { get; set; }
        public CsysReferenceOffset CsysReference { get; set; }
        public CsysWRT CsysRelative { get; set; }
    }
}
