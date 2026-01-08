using SMSgroup.Aveva.Application.CLI.Settings.Utility;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Utilities.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;
using System.Xml.Serialization;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Utility
{
    /// <summary>
    /// CLI command to scan files or folders and output file information.
    /// </summary>
    public sealed class FileInfoScanCommand : Command<FileInfoScanSettings>
    {
        public override int Execute(CommandContext context, FileInfoScanSettings settings)
        {
            try
            {
                // 1. Resolve list of files
                var files = ResolveFiles(settings);

                // 2. Build DTOs
                var results = files
                    .Select(path => FileInfoHelper.CreateFromPath(path))
                    .ToList();

                // 3. Output
                switch (settings.Output.ToLowerInvariant())
                {
                    case "json":
                        PrintJson(results);
                        break;

                    case "xml":
                        PrintXml(results);
                        break;

                    case "console":
                    default:
                        PrintConsole(results);
                        break;
                }

                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error:[/] {ex.Message}");
                return -1;
            }
        }

        // -----------------------------
        // File resolution
        // -----------------------------
        private static IEnumerable<string> ResolveFiles(FileInfoScanSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Path))
                throw new ArgumentException("Path must be provided (--path).");

            if (File.Exists(settings.Path))
                return new[] { settings.Path };

            if (Directory.Exists(settings.Path))
            {
                var searchOption = settings.Recursive
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                var files = Directory.EnumerateFiles(settings.Path, settings.Pattern, searchOption);

                if (settings.MaxFiles.HasValue)
                    files = files.Take(settings.MaxFiles.Value);

                return files;
            }

            throw new FileNotFoundException($"Path does not exist: {settings.Path}");
        }

        // -----------------------------
        // Output methods
        // -----------------------------
        private static void PrintConsole(List<FileInfoDTO> results)
        {
            var table = new Table().Border(TableBorder.Rounded).BorderColor(Spectre.Console.Color.Grey);
            table.AddColumn("File");
            table.AddColumn("Size (MB)");
            table.AddColumn("Lines");
            table.AddColumn("Writable");

            foreach (var dto in results)
            {
                table.AddRow(
                    dto.FileName,
                    dto.SizeInMB().ToString("F2"),
                    dto.LineCount.ToString(),
                    dto.IsWritable ? "[green]Yes[/]" : "[red]No[/]"
                );
            }

            AnsiConsole.Write(table);
        }

        private static void PrintJson(List<FileInfoDTO> results)
        {
            var json = JsonSerializer.Serialize(results, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            Console.WriteLine(json);
        }

        private static void PrintXml(List<FileInfoDTO> results)
        {
            var serializer = new XmlSerializer(typeof(List<FileInfoDTO>));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, results);
                Console.WriteLine(writer.ToString());
            }
        }
    }
}
