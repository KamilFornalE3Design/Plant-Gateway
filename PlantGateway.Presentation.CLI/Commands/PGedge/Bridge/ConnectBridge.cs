using SMSgroup.Aveva.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Bridge
{
    public class ConnectBridge : AsyncCommand<ConnectBridge.Settings>
    {
        private readonly SessionManager _sessionManager;

        public ConnectBridge(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public class Settings : CommandSettings
        {
            [CommandArgument(0, "<pipe>")]
            public string Pipe { get; set; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            bool success = await _sessionManager.Connect(settings.Pipe);

            if (success)
            {
                AnsiConsole.MarkupLine($"[green]✅ Connected to pipe:[/] {settings.Pipe}");
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]❌ Failed to connect to pipe:[/] {settings.Pipe}");
                return 1;
            }
        }
    }


    //public class ConnectCommand : Command<ConnectCommand.Settings>
    //{
    //    private readonly SessionManager _sessionManager;

    //    public ConnectCommand(SessionManager sessionManager)
    //    {
    //        _sessionManager = sessionManager;
    //    }

    //    public class Settings : CommandSettings
    //    {
    //        [CommandArgument(0, "<pipe>")]
    //        public string Pipe { get; set; }
    //    }

    //    public override int Execute(CommandContext context, Settings settings)
    //    {
    //        bool success = _sessionManager.Connect(settings.Pipe);

    //        if (success)
    //        {
    //            AnsiConsole.MarkupLine($"[green]✅ Connected to pipe:[/] {settings.Pipe}");
    //            return 0;
    //        }
    //        else
    //        {
    //            AnsiConsole.MarkupLine($"[red]❌ Failed to connect to pipe:[/] {settings.Pipe}");
    //            return 1;
    //        }
    //    }
    //}
}
