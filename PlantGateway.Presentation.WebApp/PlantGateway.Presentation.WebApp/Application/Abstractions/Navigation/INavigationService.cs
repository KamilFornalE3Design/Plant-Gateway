namespace PlantGateway.Presentation.WebApp.Application.Abstractions.Navigation
{
    // Application/Abstractions/Navigation/INavigationService.cs
    public interface INavigationService
    {
        void NavigateTo(string uri, bool forceLoad = false);

        // Optional helpers – add only if you want them:
        void NavigateToHome();
        void NavigateToLogin();
    }
}
