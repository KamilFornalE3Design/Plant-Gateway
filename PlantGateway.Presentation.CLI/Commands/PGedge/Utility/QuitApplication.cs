using Spectre.Console;
using Spectre.Console.Cli;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Utility
{
    public class QuitApplication : Command<QuitApplication.Settings>
    {
        public QuitApplication()
        {

        }

        public class Settings : CommandSettings
        {
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            AnsiConsole.MarkupLine("[green]Shutting down PGEdge and closing sessions...[/]");

            try
            {
                AnsiConsole.MarkupLine($"[green]<Not Implemented> Quit Application with bridges cleanup.[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to cleanup: {ex.Message}[/]");
            }

            System.Environment.Exit(0); // force quit without throwing

            return 0; // unreachable, but required
        }
    }
}
