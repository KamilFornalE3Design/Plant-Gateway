using PlantGateway.Presentation.WebApp.Application.Contracts.Search;

namespace PlantGateway.Presentation.WebApp.Application.Abstractions.Search
{
    // Application/Abstractions/Search/IGlobalSearchContributor.cs
    public interface IGlobalSearchContributor
    {
        bool CanHandle(string query);
        Task AddResultsAsync(string query, List<SearchResultGroup> sink, CancellationToken cancellationToken = default);
    }
}
