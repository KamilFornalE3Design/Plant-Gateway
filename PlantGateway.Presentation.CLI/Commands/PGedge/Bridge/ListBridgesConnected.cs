using SMSgroup.Aveva.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Bridge
{
    [Description("Lists only active sessions where both CMD and RSP pipes are connected.")]
    public sealed class ListBridgesConnected : Command<ListBridgesConnected.Settings>
    {
        public sealed class Settings : CommandSettings { }

        private readonly SessionManager _sessionManager;

        public ListBridgesConnected(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            var connected = _sessionManager.GetSessionListConnected().ToList();

            if (!connected.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No active bridge sessions found.[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[green]✔ {connected.Count} connected bridge(s):[/]");
            foreach (var name in connected)
                AnsiConsole.MarkupLine($"• {name}");

            return 0;
        }
    }
}
