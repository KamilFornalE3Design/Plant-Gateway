using Microsoft.AspNetCore.Components;
using PlantGateway.Presentation.WebApp.Application.Contracts.Layout;

namespace PlantGateway.Presentation.WebApp.Application.Abstractions.Layout
{
    public interface ILayoutContextProvider
    {
        IReadOnlyList<LayoutContextDefinition> All { get; }
        LayoutContextDefinition Get(LayoutContextId id);

        LayoutContextId Current { get; }
        void SetCurrent(LayoutContextId id);
        LayoutContextId GetCurrent();

        event Action<LayoutContextId> OnChanged;
    }
}
