using PlantGateway.Application.Abstractions.Configuration.Resolver;
using PlantGateway.Core.Config.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Infrastructure.Implementations.Configuration.Resolvers
{
    public class AppEnvironmentResolver : IAppEnvironmentResolver
    {
        public static AppEnvironment ResolveAppEnv(string env)
        {
            if (string.IsNullOrWhiteSpace(env))
            {
                return Debugger.IsAttached ? AppEnvironment.Dev : AppEnvironment.Prod;
            }

            env = env.Trim().ToLowerInvariant();

            if (env == "dev" || env == "development")
                return AppEnvironment.Dev;
            if (env == "stage" || env == "staging")
                return AppEnvironment.Stage;
            if (env == "prod" || env == "production")
                return AppEnvironment.Prod;

            throw new ArgumentOutOfRangeException(nameof(env), $"Unknown environment string: '{env}'");
        }
    }
}
