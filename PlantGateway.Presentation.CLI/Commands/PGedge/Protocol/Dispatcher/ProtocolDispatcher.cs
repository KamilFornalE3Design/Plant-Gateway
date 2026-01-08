using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Protocol.Dispatcher
{
    /// <summary>
    /// Entry command for processing Windows-registered PGedge URLs
    /// such as pgedge://cli/open or pgedge://webapp/open.
    /// </summary>
    [Description("Entry point for Windows pgedge:// protocol calls.")]
    public sealed class ProtocolDispatchSettings : CommandSettings
    {
        [CommandArgument(0, "<url>")]
        [Description("Full PGedge protocol URL, e.g. pgedge://cli/open")]
        public string Url { get; set; } = string.Empty;
    }

    public sealed class ProtocolDispatch : Command<ProtocolDispatchSettings>
    {
        public override int Execute(CommandContext context, ProtocolDispatchSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Url))
            {
                AnsiConsole.MarkupLine("[red]No URL provided for protocol dispatch.[/]");
                return -1;
            }

            try
            {
                var router = new PgedgeProtocolRouter();
                return router.Route(settings.Url);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]Protocol dispatch failed:[/] {ex.Message}");
                return -1;
            }
        }
    }
}
