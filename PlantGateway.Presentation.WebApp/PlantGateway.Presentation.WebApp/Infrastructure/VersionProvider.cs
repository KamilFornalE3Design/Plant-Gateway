using System.Reflection;

namespace PlantGateway.Presentation.WebApp.Infrastructure
{
    public static class VersionProvider
    {
        // Lazy so we compute it only once per process
        private static readonly Lazy<string> _clientVersion = new(() =>
        {
            // 1) If PG_CLIENT_VERSION is set (dev or prod), ALWAYS prefer it
            var pgVersion = Environment.GetEnvironmentVariable("PG_CLIENT_VERSION");
            if (!string.IsNullOrWhiteSpace(pgVersion))
                return pgVersion;

            // 2) Check environment name
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                                  ?? Environments.Production;

            // Development: auto dev-timestamp
            if (string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase))
            {
                return "dev-" + DateTime.UtcNow.Ticks;
            }

            // 3) Fallback: assembly informational version or normal version
            var asm = Assembly.GetExecutingAssembly();

            var infoVer = asm
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            return infoVer
                   ?? asm.GetName().Version?.ToString()
                   ?? "unknown";
        });

        public static string ClientVersion => _clientVersion.Value;
    }
}
