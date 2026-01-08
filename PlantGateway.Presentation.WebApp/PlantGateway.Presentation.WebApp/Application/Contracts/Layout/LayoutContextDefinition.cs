namespace PlantGateway.Presentation.WebApp.Application.Contracts.Layout
{
    public sealed record LayoutContextDefinition(
        LayoutContextId Id,
        string DisplayName,
        string BasePath, // "/", "/admin", "/docs"
        string? Icon = null
    );
}
