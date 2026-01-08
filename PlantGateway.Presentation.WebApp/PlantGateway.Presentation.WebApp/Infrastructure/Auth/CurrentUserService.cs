using PlantGateway.Presentation.WebApp.Application.Abstractions.Auth;

namespace PlantGateway.Presentation.WebApp.Infrastructure.Auth
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        public bool IsAuthenticated => true;

        public string DisplayName => "John Doe";

        public string Email => "john.doe@example.com";
    }
}
