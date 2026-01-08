using PlantGateway.Presentation.WebApp.Application.Abstractions.Auth;
using PlantGateway.Presentation.WebApp.Application.Abstractions.Navigation;

namespace PlantGateway.Presentation.WebApp.Infrastructure.Auth
{
    public sealed class UserProfileNavigation : IUserProfileNavigation
    {
        private readonly INavigationService _navigation;

        public UserProfileNavigation(INavigationService navigation)
        {
            _navigation = navigation;
        }

        public Task NavigateToSubscriptionsAsync()
        {
            // TODO: adjust route when you know the real one
            _navigation.NavigateTo("/account/subscriptions");
            return Task.CompletedTask;
        }

        public Task NavigateToProfileAsync()
        {
            // TODO: adjust route when you know the real one
            _navigation.NavigateTo("/account/profile");
            return Task.CompletedTask;
        }
    }
}
