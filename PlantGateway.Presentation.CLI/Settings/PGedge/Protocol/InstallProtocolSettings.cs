using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSgroup.Aveva.Application.CLI.Settings.Protocol
{
    public sealed class InstallProtocolSettings : CommandSettings
    {
        [CommandOption("--scheme")]
        [Description("URL scheme to register (default: pgedge).")]
        public string? Scheme { get; set; }

        [CommandOption("--target")]
        [Description("Path to PGedge CLI executable. Defaults to current process.")]
        public string? Target { get; set; }
    }
}
