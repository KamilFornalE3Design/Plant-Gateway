using System.Windows.Forms;

namespace PlantGateway.Presentation.CLI.Notifications.Handlers
{
    public sealed class FatalNotificationHandler : NotifyIconBase
    {
        public FatalNotificationHandler(int durationMs = 5000) : base(ToolTipIcon.Error, durationMs) { }

        public override void Show(string title, string message)
        {
            base.Show(title, message);

            MessageBox.Show(
                "❌ Process was not executed correctly.\n\n" + message,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.DefaultDesktopOnly);
        }
    }
}
