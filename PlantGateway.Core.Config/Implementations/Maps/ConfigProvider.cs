using Microsoft.Extensions.Configuration;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models;
using PlantGateway.Core.Config.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace PlantGateway.Core.Config.Implementations.Maps
{
    /// <summary>
    /// Provides centralized access to configuration values defined in appsettings.json.
    /// Validates required fields during construction.
    /// </summary>
    public class ConfigProvider : IConfigProvider
    {
        private readonly IConfiguration _config;
        private ConfigEvars _currentEvars;
        public ConfigEvars CurrentEvars => _currentEvars;
        private int _evarsInitialized; // 0 = false, 1 = true (for thread-safe one-time set)

        // New UI options, to service winform, blazor and cli
        private UserInterfacesOptions _userInterfacesOptions;
        private readonly object _uiLock = new object();

        public ConfigProvider(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            //ValidateRequiredSections();
        }

        public void InitializeEvars(ConfigEvars evars)
        {
            if (evars is null)
                throw new ArgumentNullException(nameof(evars));

            // Ensure one-time initialization even under concurrency.
            if (Interlocked.Exchange(ref _evarsInitialized, 1) == 1)
                throw new InvalidOperationException("ConfigEvars has already been initialized for this process.");

            _currentEvars = evars;
        }

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
        public ConfigEvars ResolveAppSettings(AppEnvironment environment)
        {
            string sharedConfigBasePath = GetSharedConfigPath();
            string schemaFile = "appsettings.schema.json";

            string envSuffix;
            switch (environment)
            {
                case AppEnvironment.Dev: envSuffix = "dev"; break;
                case AppEnvironment.Stage: envSuffix = "stage"; break;
                case AppEnvironment.Prod: envSuffix = "prod"; break;
                default: throw new ArgumentOutOfRangeException(nameof(environment));
            }

            string appsettingsFile = $"appsettings.{envSuffix}.json";
            string appsettingsPath = Path.Combine(sharedConfigBasePath, appsettingsFile);

            string schemaPath = Path.Combine(
                sharedConfigBasePath.Replace("Development", "Production"),
                schemaFile);

            if (!File.Exists(appsettingsPath))
                throw new FileNotFoundException($"Appsettings not found: {appsettingsPath}");
            if (!File.Exists(schemaPath))
                throw new FileNotFoundException($"Schema not found: {schemaPath}");

            var json = File.ReadAllText(appsettingsPath);
            var root = Newtonsoft.Json.Linq.JObject.Parse(json);

            var configPaths = root["ConfigPaths"]?.ToObject<Dictionary<string, string>>()
                              ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Collect all *Files sections dynamically
            var fileGroups = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in root.Properties())
            {
                if (prop.Name.EndsWith("Files", StringComparison.OrdinalIgnoreCase))
                {
                    var dict = prop.Value.ToObject<Dictionary<string, string>>()
                               ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    fileGroups[prop.Name] = dict;
                }
            }

            return new ConfigEvars(
                envSuffix,
                appsettingsPath,
                schemaPath,
                configPaths,
                fileGroups
            );
        }
        // New UI options getter
        public UserInterfacesOptions GetUserInterfacesOptions()
        {
            // simple lazy init, thread-safe enough for your usage
            if (_userInterfacesOptions != null)
                return _userInterfacesOptions;

            lock (_uiLock)
            {
                if (_userInterfacesOptions != null)
                    return _userInterfacesOptions;

                var options = new UserInterfacesOptions();
                // this requires Microsoft.Extensions.Configuration.Binder package
                _config.GetSection("UserInterfaces").Bind(options);

                _userInterfacesOptions = options;
                return _userInterfacesOptions;
            }
        }
        public ConfigEvars ResolveAppSettings(string env)
        {
            var appEnv = ConfigProvider.ResolveAppEnv(env);
            return ResolveAppSettings(appEnv);
        }
        public IReadOnlyDictionary<string, string> GetDerivedPaths(AppEnvironment environment)
        {
            return ResolveAppSettings(environment).ResolvedPaths;
        }

        public string GetDerivedPath(AppEnvironment environment, string key)
        {
            var evars = ResolveAppSettings(environment);
            if (!evars.ResolvedPaths.TryGetValue(key, out var path))
                throw new KeyNotFoundException($"Derived path not found for key: {key}");
            return path;
        }

        public string GetCatRef(string geomType, string geomKey)
        {
            if (string.IsNullOrWhiteSpace(geomType))
                throw new ArgumentNullException(nameof(geomType));

            if (!_currentEvars.GeometryRulesTOP.TryGetValue(geomType, out var innerDict))
            {
                // Geometry type missing → return null if no Default
                return null;
            }

            if (!string.IsNullOrWhiteSpace(geomKey) &&
                innerDict.TryGetValue(geomKey, out var direct))
            {
                return direct;
            }

            // Fallback to Default if defined
            if (innerDict.TryGetValue("Default", out string fallback))
            {
                return fallback;
            }

            return null; // nothing found
        }





        private void ValidateRequiredSections()
        {
            string[] topSections = { "Environment", "ConfigFiles", "PlantGateway", "Aveva", "Services", "Logging", "RuntimeOptions" };

            foreach (var section in topSections)
            {
                var sec = _config.GetSection(section);
                if (sec == null || !sec.GetChildren().Any())
                    throw new InvalidOperationException($"Missing or empty required section: '{section}'");
            }
        }

        private string GetRequired(string key)
        {
            var value = _config[key];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Missing or empty config value for '{key}'");
            return value;
        }

        private bool GetBool(string key)
        {
            var value = _config[key];
            return bool.TryParse(value, out var result) && result;
        }

        // 🌍 Environment

        public string GetSharedConfigPath()
        {
            return _config["ConfigPaths:SharedConfig"]
                   ?? throw new InvalidOperationException("SharedConfig path missing in config.");
        }

        public string GetSharedConfigFile(string key)
        {
            var basePath = GetSharedConfigPath();
            var fileName = _config[$"SharedConfigFiles:{key}"];

            if (string.IsNullOrWhiteSpace(fileName))
                throw new InvalidOperationException($"SharedConfigFiles entry '{key}' is missing.");

            return Path.Combine(basePath, fileName);
        }

        // Specific convenience methods
        public string GetTakeOverPointHeaderMapPath()
        {
            return GetSharedConfigFile("TakeOverPointHeaderMap");
        }

        public string GetTakeOverPointGeometryMapPath()
        {
            return GetSharedConfigFile("TakeOverPointGeometryMap");
        }

        public string GetDisciplinesMapPath()
        {
            return GetSharedConfigFile("DisciplinesMap");
        }

        public string GetTechnicalOrderStructureMapPath()
        {
            return GetSharedConfigFile("TechnicalOrderStructureMap");
        }

        public string GetEntityMapPath()
        {
            return GetSharedConfigFile("EntityMap");
        }

        public string GetHierarchyTreeMapPath()
        {
            return GetSharedConfigFile("HierarchyTreeMap");
        }

        public string GetRoleMapPath()
        {
            return GetSharedConfigFile("RoleMap");
        }

        public string GetTokenRegexMapPath()
        {
            return GetSharedConfigFile("TokenRegexMap");
        }
        public string GetAllowedTreeMapPath()
        {
            return GetSharedConfigFile("AllowedTreeMap");
        }
        public string GetSuffixMapPath()
        {
            return GetSharedConfigFile("SuffixMap");
        }
        public string GetDisciplineHierarchyTokenMapPath()
        {
            return GetSharedConfigFile("DisciplineHierarchyTokenMap");
        }
        public string GetTakeOverPointCachePath()
        {
            return GetSharedConfigFile("TakeOverPointCache");
        }
        public string GetCodificationMapPath()
        {
            return GetSharedConfigFile("CodificationMap");
        }


        public string GetSharedDllPath() =>
            Path.Combine(GetRequired("ConfigPaths:SharedDll"));

        public string GetAvevaDllPath() =>
            Path.Combine(GetRequired("ConfigPaths:AvevaDll"));


        // ConfigFiles

        public string GetConfigFiles() =>
            GetRequired("ConfigFiles");


        // Connectors

        public string GetPgedgeDesktopConnectorName() =>
            GetRequired("Connectors:PgedgeDesktop:Connector");
        public string GetPgedgeDesktopConnectorFolder() =>
            GetRequired("Connectors:PgedgeDesktop:PublishFolder");


        // Environment Variables (Evars)
        public string GetPgedgeDesktopConnectorEvarName() => "PGEDGE_CLI_LAUNCHER";
        public string GetPgedgeDesktopConnectorEvarValue() =>
            GetRequired("EnvironmentVariables:PGEDGE_CLI_LAUNCHER");


        // 📁 PlantGateway

        public string GetPlantGatewayDbPath() =>
            GetRequired("PlantGateway:Paths:Database");

        public string GetInputHeadersMapPath() =>
            GetRequired("PlantGateway:Paths:InputHeadersMap");

        public string GetGeometryMapPath() =>
            GetRequired("PlantGateway:Paths:GeometryMap");

        public string GetLogFolderPath() =>
            GetRequired("PlantGateway:Logging:LogFolder");

        public string GetReportFolderPath() =>
            GetRequired("PlantGateway:Logging:ReportFolder");

        // 🔧 Aveva

        public string GetAvevaE3DPath(string version = "E3D3.1") =>
            GetRequired($"Aveva:Installations:{version}");

        public Dictionary<string, string> GetRequiredSharedAssemblies()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            //var section = _config.GetSection("Aveva:Assemblies");
            //foreach (var kvp in section.GetChildren())
            //{
            //    result[kvp.Key] = kvp.Value ?? "Unknown";
            //}
            return result;
        }

        public Dictionary<string, string> GetRequiredAvevaAssemblies()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var section = _config.GetSection("Aveva:Assemblies");
            foreach (var kvp in section.GetChildren())
            {
                result[kvp.Key] = kvp.Value ?? "Unknown";
            }
            return result;
        }

        public Dictionary<string, string> GetRequiredCreoAssemblies()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            //var section = _config.GetSection("Creo:Assemblies");
            //foreach (var kvp in section.GetChildren())
            //{
            //    result[kvp.Key] = kvp.Value ?? "Unknown";
            //}
            return result;
        }

        public int GetDefaultSimplificationMode()
        {
            var value = _config["Aveva:Simplification:DefaultMode"];
            return int.TryParse(value, out var mode) ? mode : 2;
        }

        // 🛠 Services

        public bool IsSqliteEnabled() =>
            GetBool("Services:Sqlite:Enabled");

        public bool IsAzureEnabled() =>
            GetBool("Services:Azure:Enabled");

        // 🧪 Runtime

        public bool ShouldShowMessageBoxes() =>
            GetBool("RuntimeOptions:ShowMessageBoxes");

        public bool IsLoggingEnabled() =>
            GetBool("RuntimeOptions:EnableLogging");

        public string GetRuntimeProfile() =>
            GetRequired("RuntimeOptions:Profile");

        // 🪵 Logging

        public string GetLogFilePath()
        {
            var folder = GetRequired("Logging:LogFile:Path");
            var file = GetRequired("Logging:LogFile:FileName");
            return Path.Combine(folder, file);
        }

        public bool UseConsoleLogging() =>
            GetBool("Logging:ConsoleOutput");

        public string GetLoggingLevel() =>
            GetRequired("Logging:MinimumLevel");

        public string GetConfigDllPath()
        {
            string dllKey = "SMSgroup.Aveva.Config";

            var basePath = GetRequired($"Environment:{dllKey}:BasePath");
            var version = GetRequired($"Environment:{dllKey}:Version");

            var expectedDllName = $"{dllKey}.dll";
            var fullDllPath = Path.Combine(basePath, expectedDllName);

            if (!File.Exists(fullDllPath))
                throw new FileNotFoundException($"❌ Required DLL not found: {fullDllPath}");

            return fullDllPath;
        }
        public string GetStandaloneDllPath()
        {
            string dllKey = "SMSgroup.Aveva.Standalone";

            var basePath = GetRequired($"Environment:{dllKey}:BasePath");
            var version = GetRequired($"Environment:{dllKey}:Version");

            var expectedDllName = $"{dllKey}.dll";
            var fullDllPath = Path.Combine(basePath, expectedDllName);

            if (!File.Exists(fullDllPath))
                throw new FileNotFoundException($"❌ Required DLL not found: {fullDllPath}");

            return fullDllPath;
        }
        public string GetPluginDllPath()
        {
            string dllKey = "SMSgroup.Aveva.Plugin";

            var basePath = GetRequired($"Environment:{dllKey}:BasePath");
            var version = GetRequired($"Environment:{dllKey}:Version");

            var expectedDllName = $"{dllKey}.dll";
            var fullDllPath = Path.Combine(basePath, expectedDllName);

            if (!File.Exists(fullDllPath))
                throw new FileNotFoundException($"❌ Required DLL not found: {fullDllPath}");

            return fullDllPath;
        }
        public string GetUtilitiesDllPath()
        {
            string dllKey = "SMSgroup.Aveva.Utilities";

            var basePath = GetRequired($"Environment:{dllKey}:BasePath");
            var version = GetRequired($"Environment:{dllKey}:Version");

            var expectedDllName = $"{dllKey}.dll";
            var fullDllPath = Path.Combine(basePath, expectedDllName);

            if (!File.Exists(fullDllPath))
                throw new FileNotFoundException($"❌ Required DLL not found: {fullDllPath}");

            return fullDllPath;
        }
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

        /// <summary>
        /// Returns the SharedConfig folder and all SharedConfigFiles (full paths).
        /// </summary>
        public IReadOnlyList<string> GetSharedConfigPaths()
        {
            var paths = new List<string>();

            // base folder
            var basePath = GetSharedConfigPath();
            if (!string.IsNullOrWhiteSpace(basePath) && Directory.Exists(basePath))
            {
                paths.Add(Path.GetFullPath(basePath));
            }

            // all files listed in SharedConfigFiles
            var section = _config.GetSection("SharedConfigFiles");
            foreach (var child in section.GetChildren())
            {
                var path = GetSharedConfigFile(child.Key); // ✅ reuse existing fnc
                if (File.Exists(path))
                {
                    paths.Add(Path.GetFullPath(path));
                }
            }

            return paths.AsReadOnly();
        }

        /// <summary>
        /// Returns all config paths (aggregates specialized providers).
        /// </summary>
        public IReadOnlyList<string> GetAllConfigPaths()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in GetSharedConfigPaths())
                paths.Add(path);

            // 🔮 Later: merge with GetSharedDllPaths(), GetAvevaDllPaths(), etc.

            return paths.ToList().AsReadOnly();
        }

    }
    public class UserInterfacesOptions
    {
        public BlazorViewerOptions BlazorViewer { get; set; } = new BlazorViewerOptions();
        public WinFormsUiOptions WinForms { get; set; } = new WinFormsUiOptions();
        public CliUiOptions Cli { get; set; } = new CliUiOptions();
    }

    // ─── Blazor Viewer ───────────────────────────────────────────

    public class BlazorViewerOptions
    {
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// "SelfHosted" or "Intranet".
        /// </summary>
        public string Mode { get; set; } = "SelfHosted";

        public BlazorViewerRoutesOptions Routes { get; set; } = new BlazorViewerRoutesOptions();

        public bool AutoUploadDiagnostics { get; set; } = true;
        public bool AutoOpenBrowser { get; set; } = true;

        public BlazorViewerSelfHostedOptions SelfHosted { get; set; } = new BlazorViewerSelfHostedOptions();
        public BlazorViewerIntranetOptions Intranet { get; set; } = new BlazorViewerIntranetOptions();
    }

    public class BlazorViewerRoutesOptions
    {
        public string DiagnosticsPage { get; set; } = "/diagnostics";
        public string UploadEndpoint { get; set; } = "/api/diagnostics/upload";
        public string HealthEndpoint { get; set; } = "/health";
    }

    public class BlazorViewerSelfHostedOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:5000";
        public string ExePath { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public int StartupTimeoutSeconds { get; set; } = 20;
    }

    public class BlazorViewerIntranetOptions
    {
        public string BaseUrl { get; set; } = "http://intranet/pgedge-viewer";
    }

    // ─── WinForms & CLI ──────────────────────────────────────────

    public class WinFormsUiOptions
    {
        public bool Enabled { get; set; } = true;
        public string ExePath { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
    }

    public class CliUiOptions
    {
        public bool Enabled { get; set; } = true;
        public string ExePath { get; set; } = string.Empty;
    }
}
