using SMSgroup.Aveva.Config.Models.ValueObjects;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.Settings.Mapping
{
    public sealed class InspectSettings : CommandSettings
    {
        [Description("Which mapping to inspect (ALL, TOP, CATREF). Default = ALL.")]
        [CommandArgument(0, "[target]")]
        [DefaultValue("ALL")]
        public string Target { get; set; } = "ALL";

        [Description("Display format (console, file). Default = console.")]
        [CommandOption("--format <FORMAT>")]
        [DefaultValue("console")]
        public string Format { get; set; } = "console";

        public MapKeys? ResolveTargetKey()
        {
            if (string.IsNullOrWhiteSpace(Target) || Target.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                return null;

            if (Enum.TryParse<MapKeys>(Target, true, out var parsed))
                return parsed;

            throw new InvalidOperationException($"❌ Unknown mapping target: {Target}");
        }
    }
}
