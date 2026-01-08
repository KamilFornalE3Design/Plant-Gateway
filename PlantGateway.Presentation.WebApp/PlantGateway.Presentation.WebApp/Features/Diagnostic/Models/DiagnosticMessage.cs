namespace PlantGateway.Presentation.WebApp.Features.Diagnostic.Models
{
    /// <summary>
    /// Flattened message row used for the "Messages" table:
    /// DTO + engine + severity + text + optional link.
    /// </summary>
    public class DiagnosticMessage
    {
        /// <summary>
        /// Optional DTO identifier (if available in the diagnostic summary).
        /// </summary>
        public Guid? DtoId { get; set; }

        /// <summary>
        /// Human-readable DTO name.
        /// </summary>
        public string DtoName { get; set; } = string.Empty;

        /// <summary>
        /// Engine name that produced this message (TokenEngineResult, etc.).
        /// </summary>
        public string Engine { get; set; } = string.Empty;

        /// <summary>
        /// Severity string: "info", "warning", "error".
        /// </summary>
        public string Severity { get; set; } = "info";

        /// <summary>
        /// User-facing message text.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional link back to source (file:///..., or internal deep-link).
        /// </summary>
        public string? Link { get; set; }

        /// <summary>
        /// Optional logical phase (Parser, Validator, Planner, Engine, etc.).
        /// This is future-proofing for when you start mapping phase-specific messages.
        /// </summary>
        public string? Phase { get; set; }
    }
}
