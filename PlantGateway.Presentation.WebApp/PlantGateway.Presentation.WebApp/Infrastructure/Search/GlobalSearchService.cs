using PlantGateway.Presentation.WebApp.Application.Abstractions.Search;
using PlantGateway.Presentation.WebApp.Application.Contracts.Search;

namespace PlantGateway.Presentation.WebApp.Infrastructure.Search
{
    public sealed class GlobalSearchService : IGlobalSearchService
    {
        public Task<IReadOnlyList<SearchResultGroup>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            // For now: no results -> your GlobalSearchBox will just show nothing
            IReadOnlyList<SearchResultGroup> result = Array.Empty<SearchResultGroup>();
            return Task.FromResult(result);
        }
    }
}
