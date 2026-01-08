using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSgroup.Aveva.Application.CLI.Settings.Protocol
{
    public sealed class PublishProtocolSettings : CommandSettings
    {
        [CommandOption("--connector")]
        [Description("Connector key to publish protocol artifacts for (default: PgedgeCli).")]
        public string? ConnectorKey { get; set; }

        [CommandOption("--publishto")]
        [Description("Override publish folder. If not set, uses Connectors:<ConnectorKey>:PublishFolder from config.")]
        public string? PublishDirectory { get; set; }

        [CommandOption("--target")]
        [Description("Explicit path to SMSgroup.Aveva.exe. If not set, uses EnvironmentVariables:PGEDGE_CLI_LAUNCHER or current process path.")]
        public string? Target { get; set; }

        [CommandOption("--scope")]
        [Description("Registry scope: 'user' or 'machine' (default: user).")]
        [DefaultValue("user")]
        public string Scope { get; set; } = "user";
    }
}
