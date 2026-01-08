using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Session
{
    public sealed class ListRunningAvevaModules : Command<ListRunningAvevaModules.Settings>
    {
        public class Settings : CommandSettings { }

        private static readonly string[] KnownAvevaModules = new[]
        {
            "des", "mon", "dra", "lex", "tags", "par", "specon", "propcon"
        };

        public override int Execute(CommandContext context, Settings settings)
        {
            var processes = Process.GetProcesses();

            var aveva = processes
                .Where(p =>
                {
                    try
                    {
                        return KnownAvevaModules.Contains(p.ProcessName.ToLower());
                    }
                    catch { return false; }
                })
                .OrderBy(p => p.Id)
                .ToList();

            if (aveva.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]⚠️ No AVEVA E3D sessions detected.[/]");
                return 0;
            }

            var table = new Table()
                .Title("[bold green]Running AVEVA E3D Sessions[/]")
                .AddColumn("Process")
                .AddColumn("PID")
                .AddColumn("Pipe Name");

            foreach (var proc in aveva)
            {
                table.AddRow(
                    proc.ProcessName,
                    proc.Id.ToString(),
                    $"Aveva_{proc.Id}"
                );
            }

            AnsiConsole.Write(table);
            return 0;
        }
    }

}
