using Serilog;
using Serilog.Events;
using PlantGateway.Core.Config.Models.Contracts;

namespace PlantGateway.Infrastructure.Implementations.Logging
{

    /// <summary>
    /// Maps raw configuration contracts into strongly typed Serilog settings.
    /// </summary>
    public static class SerilogMapper
    {
        public static SerilogSettings Map(SerilogContract contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));

            var settings = new SerilogSettings
            {
                MinimumLevel = ParseLogEventLevel(contract.MinimumLevel),
                EnableConsole = contract.UserLog?.EnableConsole ?? true,
                RollingInterval = ParseRollingInterval(contract.AdminLog?.RollingInterval),

                UserLogPath = ResolveUserLogPath(contract.UserLog?.FileName),
                UserLogFormat = ParseLogFormat(contract.UserLog?.Format, LogFormat.Text),

                AdminLogPath = ResolveAdminLogPath(contract.AdminLog?.Path, contract.AdminLog?.FileName),
                AdminLogFormat = ParseLogFormat(contract.AdminLog?.Format, LogFormat.Json)
            };

            return settings;
        }

        private static LogEventLevel ParseLogEventLevel(string raw) =>
            Enum.TryParse<LogEventLevel>(raw, true, out var level)
                ? level
                : LogEventLevel.Information;

        private static RollingInterval ParseRollingInterval(string raw) =>
            Enum.TryParse<RollingInterval>(raw, true, out var interval)
                ? interval
                : RollingInterval.Day;

        private static LogFormat ParseLogFormat(string raw, LogFormat fallback) =>
            Enum.TryParse<LogFormat>(raw, true, out var format) ? format : fallback;

        private static string ResolveUserLogPath(string fileName)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(localAppData, "PlantGateway", "Logs");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, string.IsNullOrWhiteSpace(fileName) ? "gateway-user.log" : fileName);
        }

        private static string ResolveAdminLogPath(string path, string fileName)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return Path.Combine(path, string.IsNullOrWhiteSpace(fileName) ? "gateway-admin.log" : fileName);
        }
    }
}
