using Serilog;
using Serilog.Events;
using PlantGateway.Infrastructure.Implementations.Logging;

namespace PlantGateway.Infrastructure.Implementations.Logging
{
    /// <summary>
    /// Strongly typed runtime settings for Serilog.
    /// Produced by mapping the SerilogContract (JSON) into usable types.
    /// </summary>
    public sealed class SerilogSettings
    {
        /// <summary>
        /// Minimum log event level (Debug, Information, Warning, etc.).
        /// </summary>
        public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

        /// <summary>
        /// Whether console output is enabled for user logs.
        /// </summary>
        public bool EnableConsole { get; set; } = true;

        /// <summary>
        /// Full file path for the user log file (usually under %LocalAppData%).
        /// </summary>
        public string UserLogPath { get; set; } = string.Empty;

        /// <summary>
        /// Format of the user log (Text, Json).
        /// </summary>
        public LogFormat UserLogFormat { get; set; } = LogFormat.Text;

        /// <summary>
        /// Full file path for the admin log file (usually a shared server path).
        /// </summary>
        public string AdminLogPath { get; set; } = string.Empty;

        /// <summary>
        /// Format of the admin log (Text, Json).
        /// </summary>
        public LogFormat AdminLogFormat { get; set; } = LogFormat.Json;

        /// <summary>
        /// Rolling interval for file logs (Day, Hour, etc.).
        /// </summary>
        public RollingInterval RollingInterval { get; set; } = RollingInterval.Day;
    }
}
