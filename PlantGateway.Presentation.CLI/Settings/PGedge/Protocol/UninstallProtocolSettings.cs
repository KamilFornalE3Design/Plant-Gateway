using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSgroup.Aveva.Application.CLI.Settings.Protocol
{
    public sealed class UninstallProtocolSettings : CommandSettings
    {
        [CommandOption("--scheme")]
        [Description("URL scheme to remove (default: pgedge).")]
        public string? Scheme { get; set; }
    }
}
