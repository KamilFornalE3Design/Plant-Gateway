namespace PlantGateway.Presentation.WebApp.Features.Diagnostic.Models
{
    /// <summary>
    /// High-level overview of a pipeline phase (Parser, Validator, Planner, etc.).
    /// Used for a "process/phase" view or cards.
    /// </summary>
    public class PhaseDiagnosticsSummary
    {
        /// <summary>
        /// Phase name (Parser, Validator, Planner, Walker, Processor, Writer).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// True if the phase executed successfully.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// True if the phase considers data valid (when applicable).
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Input schema / matched schema, where meaningful (Parser/Validator).
        /// </summary>
        public string? Schema { get; set; }

        /// <summary>
        /// Source system identifier for parser, if available.
        /// </summary>
        public string? SourceSystem { get; set; }

        /// <summary>
        /// Short summary text for the phase.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// Number of warnings produced by this phase (aggregated).
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// Number of errors produced by this phase (aggregated).
        /// </summary>
        public int ErrorCount { get; set; }
    }
}
