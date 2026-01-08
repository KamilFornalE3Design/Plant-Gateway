using Spectre.Console.Cli;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SMSgroup.Aveva.Application.CLI.Settings
{
    public sealed class AvevaLaunchCommandSettings : CommandSettings
    {
        [CommandOption("--projectcode <PROJECTCODE>")]
        [Description("Company/project identifier (required).")]
        [Required]
        public string ProjectCode { get; set; } = "LUL";

        [CommandOption("--version <VERSION>")]
        [Description("Aveva version (e.g., 3.1).")]
        public string AvevaVersion { get; set; } = "3.1";

        [CommandOption("--module <MODULE>")]
        [Description("Aveva module (e.g., DESIGN, SPECON).")]
        public string AvevaModule { get; set; } = "DESIGN";

        [CommandOption("--versionint <VERSIONINT>")]
        [Description("Aveva internal version (e.g., 6).")]
        public string AvevaVersionInt { get; set; } = "6";

        [CommandOption("--location <LOCATION>")]
        [Description("Company site/entity code.")]
        public string LocationEntity { get; set; } = "DEMGL";

        [CommandOption("--userpass <USERPASS>")]
        [Description("User login (format: USER/PASS).")]
        public string AvevaUserLogin { get; set; } = "SYSTEM/SESAME";

        [CommandOption("--windowmode <MODE>")]
        [Description("Window mode (GRA or TTY).")]
        public string AvevaWindowMode { get; set; } = "GRA";

        [CommandOption("--mdb <MDB>")]
        [Description("MDB to use (default: /ALL).")]
        public string AvevaMDB { get; set; } = "/ALL";

        [CommandOption("--macro <MACRO>")]
        [Description("Macro to run after launch.")]
        public string AvevaMacro { get; set; } = string.Empty;

        [CommandOption("--launcherbat <BAT>")]
        [Description("Path to launcher BAT file.")]
        [Required]
        public string AvevaLauncherBat { get; set; } = @"L:\SMSgroup Libraries\Aveva\Launcher.bat";
    }

}
