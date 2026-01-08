namespace PlantGateway.Presentation.CLI.Notifications.Handlers
{
    public interface INotificationHandler : IDisposable
    {
        void Show(string title, string message);
    }
}
