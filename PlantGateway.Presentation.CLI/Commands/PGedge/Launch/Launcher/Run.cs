using SMSgroup.Aveva.Application.CLI.Settings;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Models;
using SMSgroup.Aveva.Standalone.Implementation;
using SMSgroup.Aveva.Standalone.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Launch.Launcher
{
    [Description("")]
    public sealed class Run : Command<AvevaLaunchCommandSettings>
    {
        public sealed class Settings : CommandSettings { }

        private readonly IConfigProvider _configProvider;

        public Run(IConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public override int Execute(CommandContext context, AvevaLaunchCommandSettings settings)
        {
            try
            {
                // Fail fast for missing settings
                if (settings.AvevaLauncherBat == string.Empty) return (int)ExitCode.InvalidArguments;

                AvevaLauncher avevaLauncher = new AvevaLauncher();
                avevaLauncher.Launch(new LauncherOptions
                {
                    ProjectCode = settings.ProjectCode,
                    AvevaVersion = settings.AvevaVersion,
                    AvevaModule = settings.AvevaModule,
                    AvevaVersionInt = settings.AvevaVersionInt,
                    LocationEntity = avevaLauncher.NormalizeLocationEntity(settings.LocationEntity),
                    AvevaUserLogin = settings.AvevaUserLogin,
                    AvevaWindowMode = avevaLauncher.NormalizeAvevaWindowMode(settings.AvevaWindowMode),
                    AvevaMDB = avevaLauncher.NormalizeAvevaMDB(settings.AvevaMDB),
                    AvevaMacro = settings.AvevaMacro,
                    AvevaLauncherBat = settings.AvevaLauncherBat
                });

                AnsiConsole.MarkupLine($"[cyan]Launching Aveva E3D {settings.AvevaModule}...[/]");

                return (int)ExitCode.Success;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]Launching failed: {ex.Message}[/]");
                return (int)ExitCode.GeneralError;
            }
        }

        private static bool ResolveEnvironment()
        {
            // If debugger is attached, assume dev
            if (Debugger.IsAttached)
                return true;

            return false;
        }
    }
}