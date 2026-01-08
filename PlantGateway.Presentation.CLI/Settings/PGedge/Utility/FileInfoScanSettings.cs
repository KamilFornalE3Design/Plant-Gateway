using Spectre.Console.Cli;
using System.ComponentModel;

namespace SMSgroup.Aveva.Application.CLI.Settings.Utility
{
    /// <summary>
    /// Options for scanning files or folders to produce FileInfoDTO output.
    /// </summary>
    public sealed class FileInfoScanSettings : CommandSettings
    {
        [CommandOption("--path <PATH>")]
        [Description("File or folder path to scan.")]
        public string Path { get; set; } = string.Empty;

        [CommandOption("--recursive")]
        [Description("If true and path is a folder, scans all subfolders.")]
        [DefaultValue(false)]
        public bool Recursive { get; set; }

        [CommandOption("--output <FORMAT>")]
        [Description("Output format: json | xml | console. Default is console.")]
        [DefaultValue("console")]
        public string Output { get; set; } = "console";

        [CommandOption("--pattern <SEARCH>")]
        [Description("Optional file search pattern (e.g. *.xml). Defaults to all files.")]
        [DefaultValue("*.*")]
        public string Pattern { get; set; } = "*.*";

        [CommandOption("--maxFiles <N>")]
        [Description("Maximum number of files to process. Defaults to no limit.")]
        public int? MaxFiles { get; set; }
    }
}
