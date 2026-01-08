using Microsoft.AspNetCore.Components;
using PlantGateway.Presentation.WebApp.Application.Abstractions.Navigation;

namespace PlantGateway.Presentation.WebApp.Infrastructure.Navigation
{
    public sealed class NavigationService : INavigationService
    {
        private readonly NavigationManager _navigationManager;

        public NavigationService(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        public void NavigateTo(string uri, bool forceLoad = false)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return;

            _navigationManager.NavigateTo(uri, forceLoad);
        }

        public void NavigateToHome()
        {
            _navigationManager.NavigateTo("/");
        }

        public void NavigateToLogin()
        {
            // TODO: adjust to your real login route
            _navigationManager.NavigateTo("/login");
        }
    }
}
