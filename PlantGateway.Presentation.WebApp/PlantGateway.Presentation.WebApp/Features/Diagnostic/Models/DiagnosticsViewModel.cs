namespace PlantGateway.Presentation.WebApp.Features.Diagnostic.Models
{
    /// <summary>
    /// In-memory representation of a single loaded diagnostic JSON file,
    /// ready for binding to the Blazor Diagnostics page.
    /// </summary>
    public class DiagnosticsViewModel
    {
        /// <summary>
        /// Top-level summary used for cards (pipeline, input, scores, counts).
        /// </summary>
        public DiagnosticsRunSummary? Summary { get; set; }

        /// <summary>
        /// Aggregated info per engine (counts of ok/missing/warning/error).
        /// Drives chips and engine matrix column definitions.
        /// </summary>
        public List<EngineSummary> EngineSummaries { get; set; } = new();

        /// <summary>
        /// DTO vs Engine status matrix rows.
        /// Drives the "Engine Matrix" view.
        /// </summary>
        public List<EngineMatrixRow> MatrixRows { get; set; } = new();

        /// <summary>
        /// Flattened messages (warnings/errors) for the "Messages" view.
        /// </summary>
        public List<DiagnosticMessage> Messages { get; set; } = new();

        /// <summary>
        /// DTO-centric overview rows (used by a "DTOs" table view).
        /// </summary>
        public List<DtoDiagnosticsRow> Dtos { get; set; } = new();

        /// <summary>
        /// Phase-level summaries (Parser, Validator, Planner, Walker, Processor, Writer).
        /// Drives a "Process / Phases" view or cards.
        /// </summary>
        public List<PhaseDiagnosticsSummary> Phases { get; set; } = new();

        /// <summary>
        /// Full path of the diagnostic file that was loaded (server side).
        /// Helpful for later DB persistence.
        /// </summary>
        public string? SourceFilePath { get; set; }

        /// <summary>
        /// When this diagnostic was loaded into memory (UTC).
        /// Lets you track how fresh the view is.
        /// </summary>
        public DateTime LoadedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Convenience flag for your UI.
        /// </summary>
        public bool HasData => Summary is not null;
    }
}
