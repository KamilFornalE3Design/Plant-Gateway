using SMSgroup.Aveva.Application.CLI.Settings;
using SMSgroup.Aveva.Config.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Environment.Project
{
    [Description("")]
    public class ListProjects : Command<AvevaProjectListCommandSettings>
    {
        public sealed class Settings : CommandSettings { }

        private readonly IConfigProvider _configProvider;

        public ListProjects(IConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public override int Execute(CommandContext context, AvevaProjectListCommandSettings settings)
        {
            AnsiConsole.MarkupLine("[cyan] List of Aveva E3D Projects: [/]");

            try
            {
                settings.ProjectList.ForEach(project =>
                {
                    AnsiConsole.MarkupLine($"[blue]• {project}[/]");
                });

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
