namespace PlantGateway.Presentation.WebApp.Configuration.Options
{
    /// <summary>
    /// UI-facing options for diagnostics files (where they are stored etc.).
    /// </summary>
    public class DiagnosticsOptions
    {
        /// <summary>
        /// Folder where .diag.json files are stored.
        /// If relative, it's resolved under ContentRootPath.
        /// </summary>
        public string StoragePath { get; set; } = "diagnostics";
    }
}
