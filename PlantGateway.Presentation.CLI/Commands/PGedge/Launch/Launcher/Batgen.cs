using SMSgroup.Aveva.Application.CLI.Settings;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Standalone.Implementation;
using SMSgroup.Aveva.Standalone.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Launch.Launcher
{
    public class Batgen : Command<AvevaLaunchCommandSettings>
    {
        public sealed class Settings : CommandSettings { }

        private readonly IConfigProvider _configProvider;

        public Batgen(IConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public override int Execute(CommandContext context, AvevaLaunchCommandSettings settings)
        {
            AnsiConsole.MarkupLine("[cyan]🔧 Aveva E3D Batgen started...[/]");

            try
            {
                AvevaBatgen avevaLauncher = new AvevaBatgen();
                avevaLauncher.Generate(new LauncherOptions
                {
                    ProjectCode = settings.ProjectCode,
                    AvevaVersion = settings.AvevaVersion,
                    AvevaModule = settings.AvevaModule,
                    AvevaVersionInt = settings.AvevaVersionInt,
                    LocationEntity = settings.LocationEntity,
                    AvevaUserLogin = settings.AvevaUserLogin,
                    AvevaWindowMode = settings.AvevaWindowMode,
                    AvevaMDB = settings.AvevaMDB,
                    AvevaMacro = settings.AvevaMacro,
                    AvevaLauncherBat = settings.AvevaLauncherBat
                });

                AnsiConsole.MarkupLine("[green]✅ Aveva E3D Bat launcher ready!.[/]");
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]❌ Aveva E3D Bat launcher failed: {ex.Message}[/]");
                return 1;
            }
        }
    }
}
