using PlantGateway.Presentation.WebApp.Application.Contracts.Help;

namespace PlantGateway.Presentation.WebApp.Application.Abstractions.Help
{
    // Application/Abstractions/Help/IHelpMenuProvider.cs
    public interface IHelpMenuProvider
    {
        Task<IReadOnlyList<HelpMenuItem>> GetItemsAsync(CancellationToken cancellationToken = default);
    }
}
