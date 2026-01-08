using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSgroup.Aveva.Application.CLI.Settings.Connector
{
    public sealed class PublishConnectorSettings : CommandSettings
    {

        [CommandOption("--connector <NAME>")]
        [Description("Logical connector name for comments/messages (default: PgedgeDesktop).")]
        public string? ConnectorName { get; init; }

        [CommandOption("--publishto <PATH>")]
        [Description("Output directory for PgedgeDesktopConnector.cmd. Defaults to the current directory.")]
        public string? PublishDirectory { get; init; }

        [CommandOption("--target <PATH_OR_URI>")]
        [Description("Optional override for the PGedge CLI path. If omitted, uses config or the current process path.")]
        public string? Target { get; init; }

        [CommandOption("--evar <NAME>")]
        [Description("Environment variable name to set (default: PGEDGE_CLI_LAUNCHER).")]
        public string? EvarName { get; init; }
    }
}
