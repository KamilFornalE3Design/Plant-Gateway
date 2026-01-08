using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PlantGateway.Infrastructure.Implementations.Configuration.Accessors
{
    public sealed class ConfigurationAccessor
    {
        private readonly IConfiguration _config;

        public ConfigurationAccessor(IConfiguration config)
        {
            _config = config;
        }

        public string GetRequired(string key)
        {
            var value = _config[key];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    $"Missing or empty config value for '{key}'");

            return value;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            var value = _config[key];
            return bool.TryParse(value, out var result)
                ? result
                : defaultValue;
        }
    }
}
