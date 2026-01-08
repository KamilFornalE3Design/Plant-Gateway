namespace PlantGateway.Presentation.WebApp.Navigation
{
    public class DocsNavigation
    {
        public sealed record Item(string Text, string Url)
        {
            public List<Item> Children { get; init; } = new();
        }

        public sealed record Section(string Title, List<Item> Items);

        public static IReadOnlyList<Section> Sections { get; } = new List<Section>
        {
            new("Getting Started", new()
            {
                new Item("Getting Started", "/docs/getting-started")
            }),

            new("Fundamentals", new()
            {
                new Item("Fundamentals", "/docs/fundamentals")
            }),

            new("Release Notes", new()
            {
                new Item("Release Notes", "/docs/release-notes")
            }),

            new("User Guide", new()
            {
                new Item("User Guide", "/docs/user-guide")
            }),

            new("Admin Guide", new()
            {
                new Item("Admin Guide", "/docs/admin-guide")
                {
                    Children =
                    {
                        new Item("Overview", "/docs/admin-guide/overview"),
                        new Item("Connectors", "/docs/admin-guide/connectors"),
                        new Item("Protocols", "/docs/admin-guide/protocols"),
                        new Item("App Settings", "/docs/admin-guide/appsettings")
                    }
                }
            }),

            new("Developer Guide", new()
            {
                new Item("Developer Guide", "/docs/developer-guide")
            }),

            new("Tutorials", new()
            {
                new Item("Tutorials", "/docs/tutorials")
            }),
        };
    }
}
