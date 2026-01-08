namespace PlantGateway.Presentation.WebApp.Features.Diagnostic.Models
{
    /// <summary>
    /// High-level summary of a single diagnostic run – used for the top cards.
    /// </summary>
    public class DiagnosticsRunSummary
    {
        /// <summary>
        /// Name of the diagnostic JSON file (e.g. ProjectStructure-6dfaf0f3.json).
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Full path to the diagnostic JSON file on disk (server side).
        /// This is useful for later DB persistence / audit.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Name of the pipeline that produced this diagnostic (e.g. ProjectStructure).
        /// </summary>
        public string PipelineName { get; set; } = string.Empty;

        /// <summary>
        /// Short pipeline id used in the file name (first 8 chars of Guid).
        /// </summary>
        public string PipelineIdShort { get; set; } = string.Empty;

        /// <summary>
        /// Full Guid assigned to the pipeline run.
        /// </summary>
        public Guid PipelineIdFull { get; set; }

        /// <summary>
        /// Timestamp from the diagnostic file (UTC).
        /// </summary>
        public DateTime TimestampUtc { get; set; }

        /// <summary>
        /// Full path of the input file that was processed (XML, CSV, etc.).
        /// </summary>
        public string InputFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Just the file name of the input (for display in cards).
        /// </summary>
        public string InputFileName => string.IsNullOrWhiteSpace(InputFilePath)
            ? string.Empty
            : Path.GetFileName(InputFilePath);

        // -----------------------------
        // Pipeline / DTO scope
        // -----------------------------

        public int TotalDtos { get; set; }
        public int ProcessedDtos { get; set; }
        public double ProcessedPercent { get; set; }

        /// <summary>
        /// Duration string as stored in JSON (e.g. "00:00:04.123").
        /// You can also parse to TimeSpan in UI if needed.
        /// </summary>
        public string Duration { get; set; } = string.Empty;

        /// <summary>
        /// Overall validity flag (your own aggregated view).
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Overall success flag (e.g. majority of engines OK, no critical errors).
        /// </summary>
        public bool IsSuccess { get; set; }

        // -----------------------------
        // Input parsing / schema info
        // -----------------------------

        public string DetectedInputSchema { get; set; } = string.Empty;
        public string SourceSystem { get; set; } = string.Empty;

        // -----------------------------
        // Data quality (scores)
        // -----------------------------

        public double TotalScore { get; set; }
        public double SyntaxScore { get; set; }
        public double SemanticScore { get; set; }
        public double CompletenessScore { get; set; }
        public double NormalizationScore { get; set; }

        // -----------------------------
        // Structural stats from parser hints
        // -----------------------------

        public int TotalNodeCount { get; set; }
        public int TotalPartNodeCount { get; set; }

        // -----------------------------
        // Message counts (for cards / badges)
        // -----------------------------

        public int MessageCount { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
    }
}
