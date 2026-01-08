using Microsoft.Extensions.Configuration;
using PlantGateway.Core.Config.Models.ValueObjects;
using System;
using System.IO;
using System.Text.Json;
using PlantGateway.Core.Config.EnvironmentVariables;


namespace PlantGateway.Core.Config.Legacy
{
    public static class ConfigHelper
    {
        private const string BootstrapFileName = "bootstrap.json";
        private const string EnvVarName = "PGEDGE_CONFIG_PATH"; // reserved for future use

        /// <summary>
        /// Entry point: load configuration for a given environment (default: prod).
        /// Uses bootstrap.json to resolve the base path.
        /// </summary>
        public static IConfigurationRoot Load(AppEnvironment env) => Load(AppEnvironmentCatalog.ToSuffix(env));

        
        /// <summary>
        /// Entry point: load configuration by environment name string.
        /// </summary>
        public static IConfigurationRoot Load(string environmentName, string cliPath = null)
        {
            environmentName = (environmentName ?? "prod").ToLowerInvariant();

            // 1️⃣ CLI path (future expansion)
            if (!string.IsNullOrWhiteSpace(cliPath) && Directory.Exists(cliPath))
                return BuildConfig(cliPath, environmentName);

            // 2️⃣ Environment variable (future expansion)
            var envPath = Environment.GetEnvironmentVariable(EnvVarName);
            if (!string.IsNullOrWhiteSpace(envPath) && Directory.Exists(envPath))
                return BuildConfig(envPath, environmentName);

            // 3️⃣ Bootstrap.json
            var bootstrapBase = ResolveBootstrapPath();
            if (!string.IsNullOrEmpty(bootstrapBase))
                return BuildConfig(bootstrapBase, environmentName);

            // 4️⃣ Known locations as last fallback
            var defaults = new[]
            {
                @"C:\Users\e3des\OneDrive - E3Design\Aplikacje\SMS Group\Development\SharedConfig",
                @"C:\Users\e3des\OneDrive - E3Design\Aplikacje\SMS Group\Production\SharedConfig",
                @"L:\SMSgroup Libraries\SharedConfig\v1.0",
                @"L:\SMSgroup Libraries\SharedConfig",
                AppContext.BaseDirectory
            };

            foreach (var candidate in defaults)
            {
                if (Directory.Exists(candidate))
                    return BuildConfig(candidate, environmentName);
            }

            throw new DirectoryNotFoundException("❌ Could not resolve any valid config path.");
        }

        /// <summary>
        /// Build IConfiguration from a resolved base path.
        /// </summary>
        private static IConfigurationRoot BuildConfig(string basePath, string environmentName)
        {
            var envFile = Path.Combine(basePath, $"appsettings.{environmentName}.json");
            if (!File.Exists(envFile))
                throw new FileNotFoundException($"❌ appsettings.{environmentName}.json not found in {basePath}");

            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: false, reloadOnChange: true)
                .Build();
        }


        /// <summary>
        /// Try to resolve config path from bootstrap.json.
        /// </summary>
        private static string ResolveBootstrapPath()
        {
            var bootstrapPath = Path.Combine(AppContext.BaseDirectory, BootstrapFileName);

            if (!File.Exists(bootstrapPath))
                return null;

            var json = File.ReadAllText(bootstrapPath);
            JsonDocument doc = null;
            try
            {
                doc = JsonDocument.Parse(json);

                JsonElement prop;
                if (!doc.RootElement.TryGetProperty("ConfigBasePath", out prop))
                    return null;

                var basePath = prop.GetString();
                if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
                    return null;

                return basePath;
            }
            finally
            {
                if (doc != null)
                    doc.Dispose();
            }
        }
    }
}
