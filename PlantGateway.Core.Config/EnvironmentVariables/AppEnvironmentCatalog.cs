using Microsoft.Extensions.Configuration;
using PlantGateway.Core.Config.Abstractions;
using PlantGateway.Core.Config.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlantGateway.Core.Config.EnvironmentVariables
{
    public class AppEnvironmentCatalog : IAppEnvironmentCatalog
    {
        #region Fields

        private readonly IConfiguration _config;

        #endregion

        #region Constructor

        public AppEnvironmentCatalog(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        /// <summary>
        /// Returns the environment variables to watch, with their descriptions.
        /// Key = variable name, Value = description.
        /// </summary>
        public IReadOnlyDictionary<string, string> GetEnvironmentVariables()
        {
            var section = _config.GetSection("EnvironmentVariables");
            return section.GetChildren()
                          .ToDictionary(x => x.Key, x => x.Value ?? string.Empty,
                                        StringComparer.OrdinalIgnoreCase);
        }

        public static string ToSuffix(AppEnvironment env)
        {
            switch (env)
            {
                case AppEnvironment.Dev: return "dev";
                case AppEnvironment.Stage: return "stage";
                case AppEnvironment.Prod: return "prod";
                default: throw new ArgumentOutOfRangeException(nameof(env));
            }
        }
    }
}
