using Microsoft.Extensions.Configuration;
using PlantGateway.Application.Abstractions.Configuration.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Infrastructure.Implementations.Configuration.Providers
{
    public class ConfigPathsProvider : IConfigPathsProvider
    {
        #region Fields

        private readonly IConfiguration _config;

        #endregion

        #region Constructor

        public ConfigPathsProvider(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        public IReadOnlyList<string> GetAllConfigPaths()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in GetSharedConfigPaths())
                paths.Add(path);

            // 🔮 Later: merge with GetSharedDllPaths(), GetAvevaDllPaths(), etc.

            return paths.ToList().AsReadOnly();
        }

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
    }
}
