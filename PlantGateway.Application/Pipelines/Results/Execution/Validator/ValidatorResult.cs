using SMSgroup.Aveva.Config.Abstractions;
using System.Collections.Generic;

namespace PlantGateway.Application.Pipelines.Results.Execution
{
    /// <summary>
    /// Represents the result of the Validator phase.
    /// Contains success state, matched schema, and detailed messages.
    /// </summary>
    public sealed class ValidatorResult
    {
        public List<IStepResult> OrderedSteps { get; } = new List<IStepResult>();
        public DataQualityScoreModel Quality { get; set; } = new DataQualityScoreModel();
        public double DataQualityScore { get; set; }



        /// <summary>
        /// Indicates whether all validation rules passed successfully.
        /// </summary>
        public bool Success { get; set; }
        public void Successs() => Success = true;

        /// <summary>
        /// Name of the schema or rule set that matched this input (e.g. "ProjectStructure").
        /// </summary>
        public string MatchedSchema { get; set; } = string.Empty;

        /// <summary>
        /// Errors found during validation (blocking issues).
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Warnings found during validation (non-blocking).
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        public List<string> Message { get; set; } = new List<string>();

        /// <summary>
        /// Helper: True if any validation errors were detected.
        /// </summary>
        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// Helper: True if any non-critical warnings were detected.
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;
        public bool IsValid { get; }

        /// <summary>
        /// Returns a formatted summary string for CLI display.
        /// </summary>
        public string Describe()
        {
            var summary = Success ? "✅ Validation Passed" : "❌ Validation Failed";
            var schema = string.IsNullOrWhiteSpace(MatchedSchema) ? "Unknown" : MatchedSchema;
            return $"{summary} | Schema={schema} | Errors={Errors.Count}, Warnings={Warnings.Count}";
        }
    }
}
