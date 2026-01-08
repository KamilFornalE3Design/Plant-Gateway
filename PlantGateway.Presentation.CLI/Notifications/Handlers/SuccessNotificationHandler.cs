using System.Windows.Forms;

namespace PlantGateway.Presentation.CLI.Notifications.Handlers
{
    public sealed class SuccessNotificationHandler : NotifyIconBase
    {
        public SuccessNotificationHandler() : base(ToolTipIcon.Info) { }
    }
}
