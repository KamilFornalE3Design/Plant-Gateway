using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.Settings
{
    public class ImportTOPCommandSettings : CommandSettings
    {
        [CommandOption("--file <FILEPATH>")]
        [Description("Full path to the TOP .txt file to import.")]
        public string FilePath { get; set; } = string.Empty;

        [CommandOption("--ref <REFERENCE>")]
        [Description("DbElement reference number or name of target EQUI.")]
        public string Reference { get; set; } = string.Empty;

        [CommandOption("--as <ALIAS>")]
        [Description("Import alias (default: EQUI).")]
        public string ImportAs { get; set; } = "EQUI";

        [CommandOption("--dry")]
        [Description("Perform a dry-run only.")]
        public bool DryRun { get; set; }

        [CommandOption("--verbose")]
        [Description("Enable detailed output.")]
        public bool Verbose { get; set; }
    }
}
