namespace PlantGateway.Presentation.WebApp.Application.Contracts.Search
{
    // Application/Contracts/Search/SearchResultGroup.cs
    public sealed record SearchResultGroup(string GroupName, IReadOnlyList<SearchResultItem> Items);
}
