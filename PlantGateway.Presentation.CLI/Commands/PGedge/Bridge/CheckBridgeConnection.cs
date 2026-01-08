using SMSgroup.Aveva.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Bridge
{
    public sealed class CheckBridgeConnection : Command<CheckBridgeConnection.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [CommandOption("--session <SESSION>")]
            [Description("Session name to check (e.g., Aveva_12345).")]
            public string SessionName { get; set; } = string.Empty;

            [CommandOption("--pipe <PIPE>")]
            [Description("Which pipe to check: CMD, RSP, or ALL (default = ALL).")]
            public string Pipe { get; set; } = "ALL";
        }

        private readonly SessionManager _sessionManager;

        public CheckBridgeConnection(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            var session = _sessionManager.GetSession(settings.SessionName);

            if (session == null)
            {
                AnsiConsole.MarkupLine($"[red]❌ Session not found: {settings.SessionName}[/]");
                return -1;
            }

            string pipeType = settings.Pipe.Trim().ToUpperInvariant();
            ExitCode exitCode = ExitCode.Fail;

            switch (pipeType)
            {
                case "CMD":
                    if (session.CheckCommandPipe())
                    {
                        AnsiConsole.MarkupLine($"[green]✅ CMD pipe connected: {settings.SessionName}[/]");
                        exitCode = (int)ExitCode.Success;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠️ CMD pipe NOT connected: {settings.SessionName}[/]");
                    }
                    break;

                case "RSP":
                    if (session.CheckResponsePipe())
                    {
                        AnsiConsole.MarkupLine($"[green]✅ RSP pipe connected: {settings.SessionName}[/]");
                        exitCode = (int)ExitCode.Success;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠️ RSP pipe NOT connected: {settings.SessionName}[/]");
                    }
                    break;

                case "ALL":
                case "DUAL":
                default:
                    if (session.CheckDualConnection())
                    {
                        AnsiConsole.MarkupLine($"[green]✅ Both pipes connected: {settings.SessionName}[/]");
                        exitCode = (int)ExitCode.Success;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠️ One or more pipes NOT connected: {settings.SessionName}[/]");
                    }
                    break;
            }

            return (int)exitCode;
        }
    }
}
