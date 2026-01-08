using PlantGateway.Presentation.WebApp.Application.Abstractions.Auth;
using PlantGateway.Presentation.WebApp.Application.Abstractions.Navigation;

namespace PlantGateway.Presentation.WebApp.Infrastructure.Auth
{
    public sealed class AuthService : IAuthService
    {
        private readonly INavigationService _navigation;

        public AuthService(INavigationService navigation)
        {
            _navigation = navigation;
        }

        public Task LogoutAsync()
        {
            // TODO later:
            // - clear tokens / cookies
            // - call sign-out endpoint (Azure AD, etc.)
            // For now: just navigate to a “logged out” / login page.

            _navigation.NavigateTo("/login", forceLoad: true);
            return Task.CompletedTask;
        }
    }
}
