using Serilog;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace PlantGateway.Infrastructure.Implementations.Logging
{
    /// <summary>
    /// Service for building and configuring Serilog loggers
    /// with user-facing and admin-facing sinks.
    /// </summary>
    public sealed class SerilogService : ISerilogService
    {
        public ILogger BuildLogger(SerilogSettings settings)
        {
            // existing combined logger (both sinks)
            var config = new LoggerConfiguration()
                .MinimumLevel.Is(settings.MinimumLevel)
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithProperty("UserName", Environment.UserName)
                .Enrich.WithProperty("ProcessId", Environment.ProcessId);

            if (settings.EnableConsole)
                config.WriteTo.Console();

            if (!string.IsNullOrWhiteSpace(settings.UserLogPath))
                config.WriteTo.File(GetFormatter(settings.UserLogFormat), settings.UserLogPath,
                    rollingInterval: settings.RollingInterval, shared: true);

            if (!string.IsNullOrWhiteSpace(settings.AdminLogPath))
                config.WriteTo.File(GetFormatter(settings.AdminLogFormat), settings.AdminLogPath,
                    rollingInterval: settings.RollingInterval, shared: true);

            var logger = config.CreateLogger();
            Log.Logger = logger;
            return logger;
        }

        /// <summary>
        /// Builds two independent loggers: one for User, one for Admin.
        /// </summary>
        public (ILogger userLogger, ILogger adminLogger) BuildSeparateLoggers(SerilogSettings settings)
        {
            // --- User logger ---
            var userConfig = new LoggerConfiguration()
                .MinimumLevel.Is(settings.MinimumLevel);

            if (settings.EnableConsole)
                userConfig.WriteTo.Console();

            if (!string.IsNullOrWhiteSpace(settings.UserLogPath))
                userConfig.WriteTo.File(
                    GetFormatter(settings.UserLogFormat),
                    settings.UserLogPath,
                    rollingInterval: settings.RollingInterval,
                    shared: true
                );

            var userLogger = userConfig.CreateLogger();

            // --- Admin logger ---
            var adminConfig = new LoggerConfiguration()
                .MinimumLevel.Is(settings.MinimumLevel)
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithProperty("UserName", Environment.UserName)
                .Enrich.WithProperty("ProcessId", Environment.ProcessId)
                .Enrich.WithProperty("UtcTimestamp", DateTime.UtcNow);

            if (!string.IsNullOrWhiteSpace(settings.AdminLogPath))
                adminConfig.WriteTo.File(
                    GetFormatter(settings.AdminLogFormat),
                    settings.AdminLogPath,
                    rollingInterval: settings.RollingInterval,
                    shared: true
                );

            var adminLogger = adminConfig.CreateLogger();

            return (userLogger, adminLogger);
        }

        private static ITextFormatter GetFormatter(LogFormat format) =>
            format switch
            {
                LogFormat.Json => new JsonFormatter(renderMessage: true),
                _ => new Serilog.Formatting.Display.MessageTemplateTextFormatter(
                        "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            };
    }
}
