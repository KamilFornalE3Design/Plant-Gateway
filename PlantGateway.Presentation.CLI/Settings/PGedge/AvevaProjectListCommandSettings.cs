using Spectre.Console.Cli;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SMSgroup.Aveva.Application.CLI.Settings
{
    public class AvevaProjectListCommandSettings : CommandSettings
    {
        [CommandOption("--projectlist <PROJECTLIST>")]
        [Description("")]
        [Required]
        public List<string> ProjectList { get; set; } = new List<string>(new string[] { "LULEA", "LUC", });
    }
}
