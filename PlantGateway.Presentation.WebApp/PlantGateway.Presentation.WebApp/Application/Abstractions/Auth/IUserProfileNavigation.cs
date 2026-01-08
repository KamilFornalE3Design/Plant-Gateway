namespace PlantGateway.Presentation.WebApp.Application.Abstractions.Auth
{
    // Application/Abstractions/Auth/IUserProfileNavigation.cs
    public interface IUserProfileNavigation
    {
        Task NavigateToSubscriptionsAsync();
        Task NavigateToProfileAsync();
    }
}
