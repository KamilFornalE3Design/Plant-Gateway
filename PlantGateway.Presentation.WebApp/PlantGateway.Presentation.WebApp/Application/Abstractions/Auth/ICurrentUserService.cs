namespace PlantGateway.Presentation.WebApp.Application.Abstractions.Auth
{
    // Application/Abstractions/Auth/ICurrentUserService.cs
    public interface ICurrentUserService
    {
        bool IsAuthenticated { get; }
        string DisplayName { get; }
        string Email { get; }
    }
}
