using SMSgroup.Aveva.Application.CLI.Settings;
using SMSgroup.Aveva.Config.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Environment.Project
{
    [Description("")]
    public class ValidateProjects : Command<AvevaProjectListCommandSettings>
    {
        public sealed class Settings : CommandSettings { }

        private readonly IConfigProvider _configProvider;

        public ValidateProjects(IConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public override int Execute(CommandContext context, AvevaProjectListCommandSettings settings)
        {
            AnsiConsole.MarkupLine("[cyan]🔧 Start[/]");

            try
            {
                settings.ProjectList.ForEach(project =>
                {
                    if (string.IsNullOrWhiteSpace(project))
                    {
                        throw new ArgumentException("Project name cannot be empty or whitespace.");
                    }

                    // Simulate validation logic
                    AnsiConsole.MarkupLine($"[yellow] -> {project}[/]");
                });

                AnsiConsole.MarkupLine("[green]✅ OK![/]");
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]❌ NOK! {ex.Message}[/]");
                return 1;
            }
        }
    }
}
