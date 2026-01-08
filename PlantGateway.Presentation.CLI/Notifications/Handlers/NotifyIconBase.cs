using System.Windows.Forms;

namespace PlantGateway.Presentation.CLI.Notifications.Handlers
{
    public abstract class NotifyIconBase : INotificationHandler
    {
        protected readonly ToolTipIcon _icon;
        protected readonly int _durationMs;

        protected NotifyIconBase(ToolTipIcon icon, int durationMs = 5000)
        {
            _icon = icon;
            _durationMs = durationMs;
        }

        public virtual void Show(string title, string message)
        {
            // Run balloon in background so CLI doesn’t block
            Task.Run(() =>
            {
                using (var ni = new NotifyIcon { Icon = SystemIcons.Information, Visible = true })
                {
                    ni.ShowBalloonTip(_durationMs, title, message, _icon);

                    // Give the system time to actually display balloon before disposing
                    Thread.Sleep(_durationMs);
                }
            });
        }

        public void Dispose() { }
    }
}
