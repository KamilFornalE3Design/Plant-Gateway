using Microsoft.Win32;
using SMSgroup.Aveva.Application.CLI.Settings.Protocol;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Protocol.Commands
{
    public sealed class UninstallProtocol : Command<UninstallProtocolSettings>
    {
        public override int Execute(CommandContext context, UninstallProtocolSettings settings)
        {
            const string scheme = "pgedge";
            var root = $@"Software\Classes\{scheme}";

            Registry.CurrentUser.DeleteSubKeyTree(root, throwOnMissingSubKey: false);

            AnsiConsole.MarkupLine("[yellow]Protocol removed (HKCU).[/]");

            return 0;
        }
    }
}
