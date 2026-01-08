namespace PlantGateway.Presentation.WebApp.Features.Diagnostic.Models
{
    public enum EngineCellStatus
    {
        Unknown,
        Ok,
        Warning,
        Error,
        Missing
    }

    /// <summary>
    /// One row in the DTO-centric matrix: DTO name + per-engine statuses.
    /// </summary>
    public class EngineMatrixRow
    {
        /// <summary>
        /// DTO identifier from the diagnostic (Guid).
        /// Useful for navigation and future DB persistence.
        /// </summary>
        public Guid? DtoId { get; set; }

        /// <summary>
        /// Human-readable DTO name (e.g. selule_cgl1_dmu).
        /// </summary>
        public string DtoName { get; set; } = string.Empty;

        /// <summary>
        /// Optional link back to the source (input file section, etc.).
        /// </summary>
        public string? Link { get; set; }

        /// <summary>
        /// Engine name → status (Ok/Warning/Error/Missing).
        /// Engine names should match EngineSummary.Name.
        /// </summary>
        public Dictionary<string, EngineCellStatus> Engines { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }
}
