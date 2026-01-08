namespace PlantGateway.Presentation.WebApp.Configuration.Options
{
    public class UserInterfacesOptions
    {
        public BlazorViewerOptions BlazorViewer { get; set; } = new();
    }

    public class BlazorViewerOptions
    {
        /// <summary>
        /// "SelfHosted" or "Intranet".
        /// </summary>
        public string Mode { get; set; } = "SelfHosted";

        /// <summary>
        /// Automatically upload diagnostics from the backend.
        /// </summary>
        public bool AutoUploadDiagnostics { get; set; } = true;

        /// <summary>
        /// Automatically open browser with the viewer UI.
        /// </summary>
        public bool AutoOpenBrowser { get; set; } = true;

        /// <summary>
        /// Display title in the Blazor UI.
        /// </summary>
        public string Title { get; set; } = "PlantGateway Diagnostics Viewer";
    }
}
