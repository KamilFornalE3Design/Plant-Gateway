namespace PlantGateway.Presentation.WebApp.Features.Diagnostic.Models
{
    /// <summary>
    /// DTO-centric overview row: per-DTO aggregated status across all engines.
    /// Perfect for a "DTOs" table view.
    /// </summary>
    public class DtoDiagnosticsRow
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// DTO type name (e.g. ProjectStructureDTO).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Link back to the source (input file / section), if present in diagnostic.
        /// </summary>
        public string? Link { get; set; }

        public int EngineOk { get; set; }
        public int EngineWarning { get; set; }
        public int EngineError { get; set; }
        public int EngineMissing { get; set; }

        public bool HasWarnings => EngineWarning > 0;
        public bool HasErrors => EngineError > 0;
    }
}
