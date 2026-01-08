using PlantGateway.Presentation.WebApp.Application.Contracts.Search;

namespace PlantGateway.Presentation.WebApp.Application.Abstractions.Search
{
    public interface IGlobalSearchService
    {
        Task<IReadOnlyList<SearchResultGroup>> SearchAsync(string query, CancellationToken cancellationToken = default);
    }
}
