using SMSgroup.Aveva.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Bridge
{
    [Description("Displays detailed connection status for all bridge sessions.")]
    public sealed class ListBridgesStatuses : Command<ListBridgesStatuses.Settings>
    {
        public sealed class Settings : CommandSettings { }

        private readonly SessionManager _sessionManager;

        public ListBridgesStatuses(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            var statuses = _sessionManager.GetStatuses().ToList();

            if (!statuses.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No sessions found.[/]");
                return 0;
            }

            var table = new Table()
                .AddColumn("Session")
                .AddColumn("Connected")
                .AddColumn("CMD Pipe")
                .AddColumn("RSP Pipe");

            foreach (var (name, isConnected, cmdPipe, rspPipe) in statuses)
            {
                table.AddRow(
                    $"[bold]{name}[/]",
                    isConnected ? "[green]✔[/]" : "[red]✘[/]",
                    cmdPipe,
                    rspPipe
                );
            }

            AnsiConsole.Write(table);
            return 0;
        }
    }

}
