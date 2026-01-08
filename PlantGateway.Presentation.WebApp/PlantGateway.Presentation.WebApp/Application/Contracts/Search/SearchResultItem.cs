namespace PlantGateway.Presentation.WebApp.Application.Contracts.Search
{
    // Application/Contracts/Search/SearchResultItem.cs
    public sealed record SearchResultItem(
        string Id,
        string Label,
        string? Description,
        string Icon,          // lucide icon name, e.g. "folder", "layers"
        string TargetRoute,   // where to navigate
        string GroupName      // e.g. "Layouts", "Projects"
    );
}
