namespace PlantGateway.Presentation.WebApp.Features.Diagnostic.Models
{
    /// <summary>
    /// Aggregated information about a single engine (Token, Position, etc.).
    /// Used for engine chips, small summaries and column generation.
    /// </summary>
    public class EngineSummary
    {
        /// <summary>
        /// Logical engine name (e.g. TokenEngineResult, PositionEngineResult).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Number of DTOs where this engine is OK/Valid (no errors, no warnings).
        /// </summary>
        public int Ok { get; set; }

        /// <summary>
        /// Number of DTOs where this engine is missing / not executed.
        /// </summary>
        public int Missing { get; set; }

        /// <summary>
        /// Number of DTOs where this engine has warnings (but no errors).
        /// </summary>
        public int Warning { get; set; }

        /// <summary>
        /// Number of DTOs where this engine has errors.
        /// </summary>
        public int Error { get; set; }

        /// <summary>
        /// Total DTOs covered by this engine.
        /// </summary>
        public int Total => Ok + Missing + Warning + Error;
    }
}
