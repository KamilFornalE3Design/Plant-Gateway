using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using SMSgroup.Aveva.Application.CLI.Settings.Protocol;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Protocol.Commands
{
    public sealed class InstallProtocol : Command<InstallProtocolSettings>
    {
        private readonly IConfiguration _config;

        public InstallProtocol(IConfiguration config) => _config = config;

        public override int Execute(CommandContext context, InstallProtocolSettings settings)
        {
            const string scheme = "pgedge";
            const string envVar = "PGEDGE_LAUNCHER";

            var exePath = Process.GetCurrentProcess().MainModule!.FileName!;

            // Set env var
            System.Environment.SetEnvironmentVariable(envVar, exePath, EnvironmentVariableTarget.User);

            // Write registry
            var root = $@"Software\Classes\{scheme}";
            using var baseKey = Registry.CurrentUser.CreateSubKey(root);
            baseKey!.SetValue("", $"{scheme.ToUpper()} URL Protocol");
            baseKey.SetValue("URL Protocol", "");

            using var cmdKey = Registry.CurrentUser.CreateSubKey($"{root}\\shell\\open\\command");
            cmdKey!.SetValue("", $"\"%{envVar}%\" \"%1\"");

            AnsiConsole.MarkupLine("[green]Protocol installed (user-level).[/]");
            return 0;
        }
    }

}
