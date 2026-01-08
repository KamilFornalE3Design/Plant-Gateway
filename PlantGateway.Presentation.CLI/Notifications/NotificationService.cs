using PlantGateway.Core.Abstractions.Contracts;
using Serilog;

namespace PlantGateway.Presentation.CLI.Notifications
{
    /// <summary>
    /// Implements centralized notification handling:
    /// - Routes logs to user/admin logs according to severity.
    /// - Delegates UI display to NotificationFactory.
    /// </summary>
    public sealed class NotificationService : INotificationService
    {
        private readonly ILogger _userLogger;
        private readonly ILogger _adminLogger;

        public NotificationService(ILogger userLogger, ILogger adminLogger)
        {
            _userLogger = userLogger;
            _adminLogger = adminLogger;
        }

        public void Notify(Severity type, string title, string message, int durationMs = 5000)
        {
            switch (type)
            {
                case Severity.Success:
                    _userLogger.Information("[SUCCESS] {Title}: {Message}", title, message);
                    break;

                case Severity.Warning:
                    _userLogger.Warning("[WARNING] {Title}: {Message}", title, message);
                    break;

                case Severity.Error:
                case Severity.Fatal:
                    // User-facing short error log
                    _userLogger.Error("[{Type}] {Title}: {Message}. Please contact admin.", type, title, message);

                    // Admin log with machine + user + UTC
                    _adminLogger.Error("[{Type}] {Title}: {Message} | Machine={Machine} | User={User} | UTC={UtcNow}",
                        type,
                        title,
                        message,
                        Environment.MachineName,
                        Environment.UserName,
                        DateTime.UtcNow);
                    break;
            }

            // Delegate UI rendering to handlers
            NotificationFactory.Show(type, title, message, durationMs);
        }
    }
}
