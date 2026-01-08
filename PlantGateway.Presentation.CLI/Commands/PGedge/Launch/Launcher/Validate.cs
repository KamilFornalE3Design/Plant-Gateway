using SMSgroup.Aveva.Application.CLI.Settings;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Standalone.Implementation;
using SMSgroup.Aveva.Standalone.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Launch.Launcher
{
    [Description("")]
    public sealed class Validate : Command<AvevaLaunchCommandSettings>
    {
        public sealed class Settings : CommandSettings { }

        private readonly IConfigProvider _configProvider;

        public Validate(IConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public override int Execute(CommandContext context, AvevaLaunchCommandSettings settings)
        {
            AnsiConsole.MarkupLine("[cyan]🔧 Launching Aveva E3D Admin...[/]");

            try
            {
                AvevaLauncher avevaLauncher = new AvevaLauncher();
                avevaLauncher.Launch(new LauncherOptions
                {
                    ProjectCode = "",
                    AvevaVersion = "",
                    AvevaModule = "",
                    AvevaVersionInt = "",
                    LocationEntity = "",
                    AvevaUserLogin = "",
                    AvevaWindowMode = "",
                    AvevaMDB = "",
                    AvevaMacro = "",
                    AvevaLauncherBat = ""
                });

                AnsiConsole.MarkupLine("[green]✅ Aveva E3D Admin is launched.[/]");
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
