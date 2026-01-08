using SMSgroup.Aveva.Utilities.Notifier.Factories;
using SMSgroup.Aveva.Utilities.Notifier.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Utility
{
    [Description("Clears the console and re-displays the CLI banner and help.")]
    public sealed class ClearConsole : Command<ClearConsole.Settings>
    {
        private readonly INotificationService _notifications;

        // Inject NotificationService via DI
        public ClearConsole(INotificationService notifications)
        {
            _notifications = notifications;
        }
        public sealed class Settings : CommandSettings { }

        public override int Execute(CommandContext context, Settings settings)
        {
            try
            {
                AnsiConsole.Clear();

                // redraw status board
                StatusBoard.RenderInitial();

                // banner
                AnsiConsole.Write(
                    new FigletText($"Plant Gateway\nAveva CLI")
                        .Centered()
                        .Color(Spectre.Console.Color.Green));

                AnsiConsole.WriteLine();

                _notifications.Notify(NotificationType.Success, "PGedge", "Operation completed successfully!");

                return 999;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks);

                _notifications.Notify(NotificationType.Error, "PGedge", "An error occurred.");

                return -1;
            }

            //_notifications.Notify(NotificationType.Warning, "PGedge", "This may cause issues...");
            //_notifications.Notify(NotificationType.Error, "PGedge", "An error occurred.");
            //_notifications.Notify(NotificationType.Fatal, "PGedge", "Critical failure! Process stopped.");
        }
    }
}
