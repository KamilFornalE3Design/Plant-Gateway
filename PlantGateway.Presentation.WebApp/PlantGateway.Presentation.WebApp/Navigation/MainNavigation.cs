namespace PlantGateway.Presentation.WebApp.Navigation
{
    public sealed record Item(string Text, string Url);
    public sealed record Section(string Title, List<Item> Items);

    public static class MainNavigation
    {
        public static IReadOnlyList<Section> Sections { get; } = new List<Section>
        {
            new("Dashboard", new() {
                new("Dashboard", "/dashboard")
            }),
            
            new("Projects", new() {
                new("Overview", "/projects/overview"),
                new("Project List", "/projects/list"),
                new("Project Explorer", "/projects/explorer")
            }),

            new("Data", new() {
                new("Overview", "/data/overview"),
                new("Pipelines", "/data/pipelines"),
                new("Mappings", "/data/mappings")
            }),

            new("Models", new() {
                new("Overview", "/models/overview"),
                new("Model Viewer", "/models/viewer")
            }),


            new("Publish", new() {
                new("Overview", "/publish/overview"),
                new("Aveva", "/publish/aveva"),
                new("Navisworks", "/publish/navisworks"),
                new("ACC", "/publish/acc")
            }),

            new("Diagnostics", new() {
                new("Overview", "/diagnostics/overview"),
                new("Diagnostics Viewer", "/diagnostics/viewer"),
                new("History", "/diagnostics/history"),
                new("Logs", "/diagnostics/logs")
            }),

        };
    }
}
