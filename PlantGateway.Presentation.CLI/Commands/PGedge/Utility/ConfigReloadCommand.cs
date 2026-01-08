using SMSgroup.Aveva.Application.CLI.Settings.Utility;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Utility
{
    [Description("Reload configuration files and environment settings manually.")]
    public sealed class ConfigReloadCommand : Command<ConfigReloadSettings>
    {
        public ConfigReloadCommand()
        {
            // DI services can be injected later if needed
        }

        public override int Execute(CommandContext context, ConfigReloadSettings settings)
        {
            try
            {
                AnsiConsole.MarkupLine("[green]✔ Reload command executed (skeleton).[/]");
                // TODO: call into IMapServiceManager.Reload(), ConfigProvider refresh, etc.
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Reload failed:[/] {ex.Message}");
                return 1;
            }
        }
    }
}
