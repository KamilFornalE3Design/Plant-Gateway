using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.Settings.Utility
{
    public sealed class ConfigReloadSettings : CommandSettings
    {
        [Description("Optional: target of reload (ALL, MAPS, EVARS). Default = ALL.")]
        [CommandArgument(0, "[target]")]
        public string Target { get; set; } = "ALL";
    }
}
