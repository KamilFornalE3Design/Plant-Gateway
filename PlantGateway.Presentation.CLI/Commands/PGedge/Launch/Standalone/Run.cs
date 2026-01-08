using SMSgroup.Aveva.Config.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Launch.Standalone
{
    [Description("")]
    public sealed class Run : Command<Run.Settings>
    {
        public sealed class Settings : CommandSettings { }

        private readonly IConfigProvider _configProvider;

        public Run(IConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            try
            {
                AnsiConsole.MarkupLine("[green]🔧 Standalone Aveva E3D Design...[/]");
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]❌ Standalone failed: {ex.Message}[/]");
                return 1;
            }
        }
    }
}
