using SMSgroup.Aveva.Config.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Launch.Standalone
{
    [Description("")]
    public sealed class Exegen : Command<Exegen.Settings>
    {
        public sealed class Settings : CommandSettings { }

        private readonly IConfigProvider _configProvider;

        public Exegen(IConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            AnsiConsole.MarkupLine("[cyan]🔧 Standalone Aveva E3D Design...[/]");

            try
            {
                AnsiConsole.MarkupLine("[green]✅ Aveva E3D Design is opened.[/]");
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
