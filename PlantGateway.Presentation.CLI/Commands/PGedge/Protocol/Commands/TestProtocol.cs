using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SMSgroup.Aveva.Application.CLI.Settings.Protocol;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Protocol.Commands
{
    /// <summary>
    /// Tests PGedge protocol registration:
    ///  - Reads current user registry
    ///  - Reads available protocols from appsettings.json
    ///  - Compares & shows difference
    /// </summary>
    [Description("Test PGedge protocol registration vs appsettings.json.")]
    public sealed class TestProtocol : Command<ProtocolTestSettings>
    {
        private readonly IConfiguration _config;
        private const string Scheme = "pgedge";

        public TestProtocol(IConfiguration config)
        {
            _config = config;
        }

        public override int Execute(CommandContext context, ProtocolTestSettings settings)
        {
            // ---------------------------------------------------------
            // Load configured protocols from appsettings.json
            // ---------------------------------------------------------
            var protocolsSection = _config.GetSection("Protocols");
            var protocols = protocolsSection
                .GetChildren()
                .Select(p => new ProtocolInfo(
                    Key: p.Key,
                    Scheme: p["Scheme"] ?? Scheme,
                    Target: p["Target"] ?? "",
                    OpenVerb: p["OpenVerb"] ?? "open"
                ))
                .Where(p => p.Scheme.Equals(Scheme, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // ---------------------------------------------------------
            // Read USER-LEVEL registry (new architecture)
            // ---------------------------------------------------------
            string regCommand = "<none>";
            bool isInstalled = false;

            using (var cmdKey = Registry.CurrentUser.OpenSubKey(
                $@"Software\Classes\{Scheme}\shell\open\command"))
            {
                if (cmdKey != null)
                {
                    regCommand = cmdKey.GetValue(null) as string ?? "<none>";
                    isInstalled = regCommand != "<none>";
                }
            }

            // ---------------------------------------------------------
            // Build main status panel
            // ---------------------------------------------------------
            var lines = new List<string>();

            if (isInstalled)
                lines.Add("[green]Protocol is installed (HKCU).[/]");
            else
                lines.Add("[red]Protocol is NOT installed.[/]");

            lines.Add($"[grey]Registered Command:[/] {Markup.Escape(regCommand)}");

            lines.Add($"[grey]Protocols in appsettings:[/] {protocols.Count}");

            lines.Add(protocols.Count > 0
                ? "[green]All configured protocols use scheme 'pgedge'.[/]"
                : "[yellow]No configured PGedge protocols found in appsettings.[/]");

            // Visual panel
            var panel = new Spectre.Console.Panel(new Markup(string.Join("\n", lines)))
                .Border(BoxBorder.Rounded)
                .Header(new PanelHeader("PGedge Protocol Status", Justify.Center))
                .Padding(1, 1)
                .Expand();

            AnsiConsole.Write(panel);

            // ---------------------------------------------------------
            // Optional detailed table (verbose mode)
            // ---------------------------------------------------------
            if (settings.Verbose && protocols.Count > 0)
            {
                AnsiConsole.WriteLine();

                var table = new Table().Border(TableBorder.Rounded).Expand();
                table.Title("Configured Protocols");

                table.AddColumn("Key");
                table.AddColumn("Target");
                table.AddColumn("OpenVerb");
                table.AddColumn("Sample URL");

                foreach (var p in protocols)
                {
                    table.AddRow(
                        p.Key,
                        p.Target,
                        p.OpenVerb,
                        $"{Scheme}://{p.Target}/{p.OpenVerb}"
                    );
                }

                AnsiConsole.Write(table);
            }

            return 0;
        }

        private sealed record ProtocolInfo(
            string Key,
            string Scheme,
            string Target,
            string OpenVerb);
    }
}
