using SMSgroup.Aveva.Application.CLI.Settings.Mapping;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Mapping
{
    /// <summary>
    /// CLI command that displays mapping configurations (TOP, CATREF, Discipline)
    /// either to console or opens files.
    /// 
    /// Modes:
    ///   console → formatted table (default)
    ///   file    → open in associated app
    ///   path    → display config file table only (no open)
    /// </summary>
    public sealed class InspectCommand : Command<InspectSettings>
    {
        private readonly IEnumerable<IMapService> _mapServices;

        public InspectCommand(IEnumerable<IMapService> mapServices)
        {
            _mapServices = mapServices ?? throw new ArgumentNullException(nameof(mapServices));
        }

        public override int Execute(CommandContext context, InspectSettings settings)
        {
            var key = settings.ResolveTargetKey();
            var format = settings.Format.ToLowerInvariant();

            var targets = key == null
                ? _mapServices
                : _mapServices.Where(s => s.Key == key.Value);

            if (!targets.Any())
            {
                AnsiConsole.MarkupLine($"[red]❌ Mapping not found for:[/] {settings.Target}");
                return 1;
            }

            switch (format)
            {
                case "file":
                    OpenFiles(targets, showOnly: false);
                    return 0;

                case "path":
                    OpenFiles(targets, showOnly: true);
                    return 0;

                default:
                    return DumpMappingsToConsole(key);
            }
        }

        #region MAIN CONSOLE DUMP

        private int DumpMappingsToConsole(MapKeys? targetKey)
        {
            var selected = targetKey == null
                ? _mapServices
                : _mapServices.Where(s => s.Key == targetKey.Value);

            if (!selected.Any())
            {
                AnsiConsole.MarkupLine($"[red]❌ No mappings found for target: {targetKey}[/]");
                return 1;
            }

            foreach (var svc in selected)
            {
                AnsiConsole.MarkupLine($"\n[bold underline]{svc.Key} Mapping[/]");
                AnsiConsole.MarkupLine($"[grey]{svc.Description}[/]\n");

                var dto = svc.GetMapUntyped();

                switch (dto)
                {
                    case CatrefMapDTO catref:
                        RenderCatrefMap(catref);
                        break;

                    case CodificationMapDTO codification:
                        RenderCodificationMap(codification);
                        break;

                    case HeaderMapDTO header:
                        RenderHeaderMap(header);
                        break;

                    case DisciplineMapDTO discipline:
                        RenderDisciplineMap(discipline);
                        break;

                    case TokenRegexMapDTO tokenRegex:
                        RenderTokenRegex(tokenRegex);
                        break;

                    default:
                        AnsiConsole.MarkupLine("[yellow]⚠ No renderer defined for this mapping type.[/]");
                        break;
                }
            }

            AnsiConsole.MarkupLine("\n[green bold]✔ Inspection completed successfully[/]");
            return 0;
        }

        #endregion

        #region RENDERERS

        private static void RenderHeaderMap(HeaderMapDTO header)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("Headers")
                .AddColumn("[cyan]Raw Header[/]")
                .AddColumn("[green]DTO Property[/]");

            foreach (var kv in header.Headings)
                table.AddRow($"[white]{kv.Key}[/]", $"[white]{kv.Value}[/]");

            AnsiConsole.Write(table);
        }

        private static void RenderCatrefMap(CatrefMapDTO catref)
        {
            RenderRulesTable("NOZZ rules", catref.Nozz);
            RenderRulesTable("ELCONN rules", catref.Elconn);
            RenderRulesTable("DATUM rules", catref.Datum);
        }

        private static void RenderDisciplineMap(DisciplineMapDTO map)
        {
            if (map?.Disciplines == null || map.Disciplines.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]❌ Discipline map is empty or not loaded properly.[/]");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Spectre.Console.Color.Grey)
                .Title("Disciplines")
                .AddColumn("[yellow]Code[/]")
                .AddColumn("[cyan]Family[/]")
                .AddColumn("[green]Order[/]")
                .AddColumn("[white]Designation[/]");

            foreach (var d in map.Disciplines.Values.OrderBy(x => x.Order))
                table.AddRow(d.Code, d.Family, d.Order.ToString(), d.Designation);

            AnsiConsole.Write(table);
        }

        private static void RenderTokenRegex(TokenRegexMapDTO map)
        {
            if (map?.TokenRegex == null || map.TokenRegex.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]❌ Token Regex map is empty or not loaded properly.[/]");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Spectre.Console.Color.Grey)
                .Title("Token Regex Map")
                .AddColumn("[yellow]Key[/]")
                .AddColumn("[cyan]Pattern[/]")
                .AddColumn("[green]Example[/]")
                .AddColumn("[white]Position[/]")
                .AddColumn("[magenta]Type[/]")
                .AddColumn("[blue]Removable[/]")
                .AddColumn("[grey]Description[/]");

            foreach (var kvp in map.TokenRegex
                                  .OrderBy(x => x.Value.Position)
                                  .ThenBy(x => x.Key))
            {
                var key = kvp.Key;
                var token = kvp.Value;

                // Escape Spectre markup-sensitive characters (brackets, etc.)
                string Safe(string input) => string.IsNullOrWhiteSpace(input)
                    ? "[dim]<none>[/]"
                    : Markup.Escape(input);

                string pattern = Safe(token.Pattern);
                string example = Safe(token.Example);

                string position = token.Position >= 0
                    ? $"{token.Position} {(token.IsDynamic ? "[dim](suffix)[/]" : "[green](base)[/]")}"
                    : "[dim]n/a[/]";

                string type = Safe(token.Type);
                string description = string.IsNullOrWhiteSpace(token.Description)
                    ? "[dim]<no description>[/]"
                    : Markup.Escape(token.Description);

                table.AddRow(key, pattern, example, position, type, description);
            }

            AnsiConsole.Write(table);
        }

        private static void RenderCodificationMap(CodificationMapDTO map)
        {
            if (map?.Codifications == null || map.Codifications.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]❌ Codification map is empty or not loaded properly.[/]");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Spectre.Console.Color.Grey)
                .Title("[bold cyan]Codification Map[/]")
                .AddColumn("[yellow]Code[/]")
                .AddColumn("[green]Type[/]");

            // Group by CodificationType to display hierarchy nicely
            foreach (var group in map.Codifications.Values
                .GroupBy(c => c.CodificationType)
                .OrderBy(g => g.Key))
            {
                table.AddRow($"[bold blue]{group.Key}[/]", string.Empty);

                foreach (var cod in group.OrderBy(c => c.Code))
                {
                    table.AddRow(cod.Code, cod.CodificationType.ToString());
                }

                // Add an empty row after each group for readability
                table.AddEmptyRow();
            }

            AnsiConsole.Write(table);
        }

        private static void RenderRulesTable(string title, Dictionary<string, string>? rules)
        {
            if (rules == null || rules.Count == 0)
                return;

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title(title)
                .AddColumn("[cyan]Key[/]")
                .AddColumn("[green]Catref[/]");

            foreach (var kv in rules)
                table.AddRow($"[white]{kv.Key}[/]", $"[white]{kv.Value}[/]");

            AnsiConsole.Write(table);
        }

        #endregion

        #region FILE OPEN LOGIC

        private void OpenFiles(IEnumerable<IMapService> services, bool showOnly)
        {
            var table = RenderConfigTable(services, showOnly);

            // showOnly = false → also open files
            if (!showOnly)
            {
                foreach (var svc in services)
                {
                    var path = svc.GetFilePath();
                    if (File.Exists(path))
                        OpenFile(path);
                }
            }

            AnsiConsole.Write(table);
        }

        /// <summary>
        /// Builds a Spectre table listing map names and config file paths.
        /// </summary>
        private static Table RenderConfigTable(IEnumerable<IMapService> services, bool showOnly)
        {
            var title = showOnly
                ? "[green]Configuration file locations[/]"
                : "[green]Opened config files[/]";

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Expand()
                .Title(title)
                .AddColumn("[cyan]Map Name[/]")
                .AddColumn("[white]Path to config file[/]");

            foreach (var svc in services)
            {
                var path = svc.GetFilePath();
                if (File.Exists(path))
                    table.AddRow($"[yellow]{svc.Key}[/]", $"[grey]{Path.GetFullPath(path)}[/]");
                else
                    table.AddRow($"[yellow]{svc.Key}[/]", "[red]❌ File not found[/]");
            }

            return table;
        }

        private static void OpenFile(string path)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Failed to open file:[/] {ex.Message}");
            }
        }

        #endregion
    }
}
