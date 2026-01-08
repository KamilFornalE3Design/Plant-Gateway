using PlantGateway.Core.Abstractions.Contracts;
using PlantGateway.Presentation.CLI.Notifications.Handlers;

namespace PlantGateway.Presentation.CLI.Notifications
{
    public static class NotificationFactory
    {
        public static void Show(Severity type, string title, string message, int durationMs = 5000)
        {
            using var handler = Create(type, durationMs);
            handler.Show(title, message);
        }

        private static INotificationHandler Create(Severity type, int durationMs) =>
            type switch
            {
                Severity.Success => new SuccessNotificationHandler(),
                Severity.Warning => new WarningNotificationHandler(),
                Severity.Error => new ErrorNotificationHandler(),
                Severity.Fatal => new FatalNotificationHandler(),
                _ => new SuccessNotificationHandler()
            };
    }
}
