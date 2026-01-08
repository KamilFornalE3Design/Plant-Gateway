using SMSgroup.Aveva.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Bridge
{
    [Description("Lists all known Aveva pipe bridge sessions, connected or not.")]
    public sealed class ListBridgesAll : Command<ListBridgesAll.Settings>
    {
        public sealed class Settings : CommandSettings { }

        private readonly SessionManager _sessionManager;

        public ListBridgesAll(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            var sessions = _sessionManager.GetAllSessions().ToList();

            if (sessions.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No bridge sessions found.[/]");
                return 0;
            }

            var table = new Table()
                .AddColumn("Session")
                .AddColumn("Bridge Connected")
                .AddColumn("CMD Pipe")
                .AddColumn("RSP Pipe");

            foreach (var s in sessions)
            {
                var cmdColor = s.CmdServer.IsClientConnected ? "green" : "red";
                var rspColor = s.RspServer.IsClientConnected ? "green" : "red";

                table.AddRow(
                    $"[bold]{s.SessionName}[/]",
                    s.IsConnected ? "[green]YES[/]" : "[red]NO[/]",
                    $"[{cmdColor}]{s.CommandPipeName}[/]",
                    $"[{rspColor}]{s.ResponsePipeName}[/]"
                );
            }

            AnsiConsole.Write(table);
            return 0;
        }
    }
}
