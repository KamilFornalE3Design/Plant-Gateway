using System.Windows.Forms;

namespace PlantGateway.Presentation.CLI.Notifications.Handlers
{
    public sealed class WarningNotificationHandler : NotifyIconBase
    {
        public WarningNotificationHandler() : base(ToolTipIcon.Warning) { }
    }
}
