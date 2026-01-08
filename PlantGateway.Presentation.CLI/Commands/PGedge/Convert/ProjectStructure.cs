using SMSgroup.Aveva.Application.CLI.Helpers;
using SMSgroup.Aveva.Application.CLI.Settings.Convert;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.Diagnostic;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Utilities.Notifier.Factories;
using SMSgroup.Aveva.Utilities.Notifier.Interfaces;
using SMSgroup.Aveva.Utilities.Pipeline;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.Contracts;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Convert
{
    public class ProjectStructure : Command<ConvertSettings>
    {
        private PipelineContract<ProjectStructureDTO> _pipeline;
        private IPipelineCoordinator<ProjectStructureDTO> _coordinator;
        private readonly IPipelineDiagnoser<ProjectStructureDTO> _diagnoser;

        private readonly INotificationService _notifications;

        public ProjectStructure(PipelineContract<ProjectStructureDTO> pipeline, IPipelineCoordinator<ProjectStructureDTO> coordinator, IPipelineDiagnoser<ProjectStructureDTO> diagnoser, INotificationService notifications)
        {
            _pipeline = pipeline;
            _coordinator = coordinator;
            _diagnoser = diagnoser;
            _notifications = notifications;
        }
        public override int Execute(CommandContext context, ConvertSettings settings)
        {
            try
            {
                // Setup start time
                DateTime processStart = DateTime.UtcNow;

                // ─────────────────────────────
                //  Step 0: Build Input / Output
                // ─────────────────────────────
                _pipeline.Input = TargetBuilder.BuildInput(settings);
                _pipeline.Output = TargetBuilder.BuildOutput(settings, _pipeline.Input, typeof(ProjectStructureDTO));

                // ─────────────────────────────
                //  Step 1: Determine Phase
                // ─────────────────────────────
                _pipeline.Phase = ProcessPhaseExtensions.ParsePhase(settings.Phase);

                // ─────────────────────────────
                //  Step 1a: Determine Csys Options
                // ─────────────────────────────
                _pipeline.CsysWRT = ProcessCsysSettings.GetCsysWRT(settings.CsysWRT);
                _pipeline.CsysReferenceOffset = ProcessCsysSettings.GetCsysReferenceOffset(settings.CsysReferenceOffset);
                _pipeline.CsysOption = ProcessCsysSettings.GetCsysOption(settings.CsysOption);

                // ─────────────────────────────
                //  Step 2: Delegate to Coordinator
                // ─────────────────────────────
                var result = _coordinator.Run(_pipeline.Phase);

                // ─────────────────────────────
                //  Step 3: Display & Return Code
                // ─────────────────────────────
                var diagnosis = _diagnoser.DiagnoseAsync(_pipeline);

                // Optional: show summary (uses your RenderDiagnosticSummary)
                //RenderDiagnosticSummary(diagnosis.Diagnostics.Summary);

                // Show where JSON was written
                //AnsiConsole.MarkupLineInterpolated(
                //    $"[green]📄 Diagnostic saved:[/] [blue]{diagnosis.}[/]");

                _notifications.Notify(NotificationType.Success, "PGedge", "Operation completed successfully!");

                return result.ExitCode;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]❌ CLI Exception:[/] {ex.Message}");
                if (ex.InnerException != null)
                    AnsiConsole.MarkupLineInterpolated($"[grey]→ Inner:[/] {ex.InnerException.Message}");

                _notifications.Notify(NotificationType.Fatal, "PGedge", "Operation failed!");

                return -1;
            }
        }

        public static void RenderDiagnosticSummary(DiagnosticSummary summary)
        {
            if (summary == null)
            {
                AnsiConsole.MarkupLine("[red]❌ No diagnostic summary available.[/]");
                return;
            }

            // ─────────────────────────────
            //  Flat numbers
            // ─────────────────────────────
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold yellow]📊 Diagnostic Summary[/]")
                .AddColumn("[grey]Metric[/]")
                .AddColumn("[grey]Value[/]");

            table.AddRow("Total DTOs", $"{summary.TotalDtos}");
            table.AddRow("Processed DTOs", $"{summary.ProcessedDtos}");
            table.AddRow("Processed %", $"{summary.ProcessedPercent:F1}%");
            table.AddRow("Duration", $"{summary.Duration.TotalSeconds:F2}s");
            AnsiConsole.Write(table);

            // ─────────────────────────────
            //  Engine Coverage
            // ─────────────────────────────
            if (summary.Engines.Any())
            {
                var engTable = new Table()
                    .Border(TableBorder.Minimal)
                    .Title("[bold cyan]Engine Coverage[/]")
                    .AddColumn("Engine")
                    .AddColumn("Ok")
                    .AddColumn("Missing");

                foreach (var kvp in summary.Engines)
                    engTable.AddRow(kvp.Key, kvp.Value.Ok.ToString(), kvp.Value.Missing.ToString());

                AnsiConsole.Write(engTable);
            }

            // ─────────────────────────────
            //  Issues: Warnings / Errors
            // ─────────────────────────────
            if (summary.Warnings.Any() || summary.Errors.Any() || summary.Unprocessed.Any())
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold yellow]⚠ Warnings[/]");
                foreach (var w in summary.Warnings.Take(10))
                    AnsiConsole.MarkupLine($"  • [yellow]{w.Engine}[/] – [link={w.Link}]{w.DtoName}[/]: {w.Message}");

                if (summary.Warnings.Count > 10)
                    AnsiConsole.MarkupLine($"  ...and {summary.Warnings.Count - 10} more warnings.");

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold red]❌ Errors[/]");
                foreach (var e in summary.Errors.Concat(summary.Unprocessed).Take(10))
                    AnsiConsole.MarkupLine($"  • [red]{e.Engine}[/] – [link={e.Link}]{e.DtoName}[/]: {e.Message}");

                if (summary.Errors.Count + summary.Unprocessed.Count > 10)
                    AnsiConsole.MarkupLine($"  ...and {(summary.Errors.Count + summary.Unprocessed.Count - 10)} more errors.");
            }
            else
            {
                AnsiConsole.MarkupLine("[green]✅ No warnings or errors detected.[/]");
            }

            AnsiConsole.WriteLine();
        }
    }
}
