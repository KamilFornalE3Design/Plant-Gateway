using SMSgroup.Aveva.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Bridge
{
    public class DisconnectBridge : Command<DisconnectBridge.Settings>
    {
        private readonly SessionManager _sessionManager;

        public DisconnectBridge(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public class Settings : CommandSettings
        {
            [CommandArgument(0, "<pipe>")]
            public string Pipe { get; set; }
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            bool success = _sessionManager.Disconnect(settings.Pipe);

            if (success)
            {
                AnsiConsole.MarkupLine($"[green]✅ Disconnected from pipe:[/] {settings.Pipe}");
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]❌ Failed to disconnect from pipe:[/] {settings.Pipe}");
                return 1;
            }
        }
    }
}
