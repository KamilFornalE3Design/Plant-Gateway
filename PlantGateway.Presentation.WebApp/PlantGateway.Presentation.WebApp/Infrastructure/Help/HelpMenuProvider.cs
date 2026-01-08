using PlantGateway.Presentation.WebApp.Application.Abstractions.Help;
using PlantGateway.Presentation.WebApp.Application.Contracts.Help;

namespace PlantGateway.Presentation.WebApp.Infrastructure.Help
{
    public sealed class HelpMenuProvider : IHelpMenuProvider
    {
        private static readonly HelpMenuItem[] _items =
        {
        // internal help landing page
        new(
            Id: "help",
            Label: "Help",
            TargetUrl: "/help",
            IsExternal: false
        ),

        // user guide, for now pretend it is external docs
        new(
            Id: "user-guide",
            Label: "User Guide",
            TargetUrl: "https://your-company-docs/plant-gateway/user-guide",
            IsExternal: true
        ),

        // contact service / support
        new(
            Id: "contact-service",
            Label: "Contact Service",
            TargetUrl: "/support/contact",
            IsExternal: false
        )
    };

        public Task<IReadOnlyList<HelpMenuItem>> GetItemsAsync(
            CancellationToken cancellationToken = default)
        {
            // Could add per-role filtering here later
            return Task.FromResult<IReadOnlyList<HelpMenuItem>>(_items);
        }
    }
}
