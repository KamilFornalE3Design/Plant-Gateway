using PlantGateway.Core.Abstractions.Contracts;

namespace PlantGateway.Presentation.CLI.Notifications
{
    /// <summary>
    /// Centralized service for raising user/admin notifications.
    /// Responsible for logging (user vs admin streams) 
    /// and delegating UI display to NotificationFactory.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a notification of the given type with title and message. 
        /// Handles logging and delegates UI rendering.
        /// </summary>
        /// <param name="type">The severity of the notification.</param>
        /// <param name="title">Notification title.</param>
        /// <param name="message">Notification message.</param>
        /// <param name="durationMs">Balloon duration in ms (default 5s).</param>
        void Notify(Severity type, string title, string message, int durationMs = 5000);
    }
}
