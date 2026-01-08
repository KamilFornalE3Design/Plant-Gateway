using SMSgroup.Aveva.Config.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Launch.Standalone
{
    [Description("")]
    public sealed class Validate : Command<Validate.Settings>
    {
        public sealed class Settings : CommandSettings { }

        private readonly IConfigProvider _configProvider;

        public Validate(IConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            try
            {
                AnsiConsole.MarkupLine("[green]✅ NOT IMPLEMENTED Set of validation functions of Aveva Standalone.[/]");

                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]❌ Launching failed: {ex.Message}[/]");
                return 1;
            }
        }
    }
}
