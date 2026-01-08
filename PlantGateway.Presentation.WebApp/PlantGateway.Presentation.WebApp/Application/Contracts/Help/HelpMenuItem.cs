namespace PlantGateway.Presentation.WebApp.Application.Contracts.Help
{
    // Application/Contracts/Help/HelpMenuItem.cs
    public sealed record HelpMenuItem(
        string Id,
        string Label,
        string TargetUrl,
        bool IsExternal);

}
