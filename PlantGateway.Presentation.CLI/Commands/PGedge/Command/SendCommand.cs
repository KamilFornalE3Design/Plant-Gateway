using SMSgroup.Aveva.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Command
{
    public class SendCommand : Command<SendCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandArgument(0, "<SESSION>")]
            public string Session { get; set; } = string.Empty;

            [CommandArgument(1, "<COMMAND>")]
            public string Command { get; set; } = string.Empty;

            [CommandOption("--dryrun")]
            public bool DryRun { get; set; }
        }

        private readonly SessionManager _sessionManager;

        public SendCommand(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            string input = settings.Command;
            string session = settings.Session;

            if (string.IsNullOrWhiteSpace(input))
                return Fail("Command cannot be empty.");

            var parts = input.Split(new[] { "::" }, 2, StringSplitOptions.None);
            if (parts.Length != 2)
                return Fail("Invalid format. Use <DESCRIPTOR>::<PAYLOAD>");

            var descriptor = parts[0].Trim().ToUpperInvariant();
            var payload = parts[1].Trim();

            const string Separator = "|";
            bool HasMultilineMarker(string str) => str.Contains(Separator);

            // ❗ Block usage of "|" unless descriptor is PMLBLOCK
            if (HasMultilineMarker(payload) && descriptor != "PMLBLOCK")
            {
                return Fail(
                    $"Multi-line marker '{Separator}' detected, but descriptor is not PMLBLOCK.\n\n" +
                    $"Only PMLBLOCK supports multi-line input using '{Separator}' as separator.\n" +
                    $"Example:\n  PMLBLOCK::LINE1{Separator}LINE2{Separator}LINE3"
                );
            }

            return (descriptor, string.IsNullOrWhiteSpace(payload)) switch
            {
                (_, true) => Fail("Payload after '::' is missing or empty."),
                ("COMMAND", false) or
                ("PMLLINE", false) or
                ("PMLBLOCK", false) or
                ("PMLMAC", false) or
                ("MODULE", false) or
                ("SWITCH", false)
                    => Send(),

                _ => Fail($"Unknown descriptor: '{descriptor}'. Allowed: COMMAND, PMLLINE, PMLBLOCK, PMLMAC, MODULE, SWITCH")
            };

            int Send()
            {
                if (settings.DryRun)
                {
                    AnsiConsole.MarkupLine("[yellow]Dry run mode active. Command will not be sent.[/]");
                    AnsiConsole.WriteLine($"Session: {session ?? "(none)"}");
                    AnsiConsole.WriteLine($"Command: {input}");
                    AnsiConsole.WriteLine($"Descriptor: {descriptor}");
                    AnsiConsole.WriteLine("Payload:");

                    var lines = descriptor == "PMLBLOCK"
                        ? payload.Split(new[] { Separator }, StringSplitOptions.None)
                        : new[] { payload };

                    foreach (var line in lines)
                        AnsiConsole.WriteLine($"   {line}");

                    return 0;
                }

                if (string.IsNullOrWhiteSpace(session))
                    return Fail("No session specified. Use --session <name> or --dryrun to simulate.");

                _sessionManager.Send(session, input);
                return 0;
            }

            int Fail(string message)
            {
                AnsiConsole.MarkupLine($"[red]{Escape(message)}[/]");
                return -1;
            }

            string Escape(string text) => Markup.Escape(text);
        }
    }
}
