using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSgroup.Aveva.Application.CLI.Settings.Protocol
{
    public sealed class ProtocolTestSettings : CommandSettings
    {
        [CommandOption("--verbose|-v")]
        [Description("Show detailed tables (config + registry).")]
        public bool Verbose { get; set; }
    }
}
