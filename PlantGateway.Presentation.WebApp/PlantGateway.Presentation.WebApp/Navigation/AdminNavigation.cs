namespace PlantGateway.Presentation.WebApp.Navigation
{
    public static class AdminNavigation
    {
        public sealed record Item(string Text, string Url);
        public sealed record Section(string Title, List<Item> Items);

        public static IReadOnlyList<Section> Sections { get; } = new List<Section>
        {
            new("Dashboard", new() {
                new("Dashboard", "/admin")
            }),
            new("Projects", new() {
                new("Projects", "/admin/projects")
            }),
            new("Users", new() {
                new("Users", "/admin/users")
            }),
            new("Teams", new() {
                new("Teams", "/admin/teams")
            }),
            new("Software", new() {
                new("Software", "/admin/software")
            }),
            new("Data Bases", new() {
                new("Data Bases", "/admin/databases")
            }),
            new("Diagnostics", new() {
                new("Diagnostics", "/admin/diagnostics")
            }),
        };
    }
}
