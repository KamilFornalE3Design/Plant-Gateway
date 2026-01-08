using PlantGateway.Presentation.WebApp.Configuration.Options;

namespace PlantGateway.Presentation.WebApp.Configuration.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers application options (bound from appsettings.json) for the Blazor UI.
        /// </summary>
        public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind the "UserInterfaces" section to UserInterfacesOptions
            services.Configure<UserInterfacesOptions>(configuration.GetSection("UserInterfaces"));

            // Bind the "Diagnostics" options (will use defaults if section is missing)
            services.Configure<DiagnosticsOptions>(configuration.GetSection("Diagnostics"));

            // In future you can add more:
            // services.Configure<DiagnosticsOptions>(configuration.GetSection("Diagnostics"));
            // services.Configure<SomeFeatureOptions>(configuration.GetSection("Features:SomeFeature"));

            return services;
        }
    }
}
