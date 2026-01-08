using SMSgroup.Aveva.Config.Models.CLIOptions.TOP;
using SMSgroup.Aveva.Config.Models.PlannerBlocks.Position;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SMSgroup.Aveva.Application.CLI.Settings.Convert
{
    /// <summary>
    /// Common settings for all convert commands - they are all the same in principle
    /// The difference lays in the 3rd part of command (TOP / MSCAD / SIMPLIFY / MCAD ...)
    /// </summary>
    public class ConvertSettings : CommandSettings
    {
        [CommandOption("-f|--filefullpath")]
        [Description("Full path to the .asm.txt file to process.")]
        [Required]
        public string FileFullPath { get; set; }

        [CommandOption("-s|--secondaryfile")]
        [Description("Secondary file for comparison (only used in --mode compare).")]
        public string SecondaryFile { get; set; }

        [CommandOption("--format")]
        [Description("Output format: console, json, txt, pmlmac, xml")]
        [DefaultValue("console")]
        public string FormatRaw { get; set; } = "console";

        public OutputDataFormat Format => Enum.TryParse<OutputDataFormat>(FormatRaw, true, out var parsed) ? parsed : OutputDataFormat.console;

        public OutputSinkType SinkType
        {
            get
            {
                switch (Format)
                {
                    case OutputDataFormat.console:
                        return OutputSinkType.Console;

                    case OutputDataFormat.txt:
                    case OutputDataFormat.json:
                    case OutputDataFormat.xml:
                    case OutputDataFormat.pmlmac:
                        return OutputSinkType.File;

                    // for later
                    // case OutputDataFormat.db: return OutputSinkType.Db;

                    default:
                        return OutputSinkType.Console; // safe default
                }
            }
        }

        [Description("Execution phase: parse, validate, or full. Default=full.")]
        [CommandOption("--phase <PHASE>")]
        [DefaultValue("full")]
        public string Phase { get; set; } = "full";

        // Coordinate System Option
        [Description("Specifies which coordinate system mode to use: absolute, global, relative, transformed, or withoffset. Default=relative.")]
        [CommandOption("--CsysOption <MODE>")]
        [DefaultValue("relative")]
        public string CsysOption { get; set; } = "relative";

        // Coordinate System Reference Offset
        [Description("Optional custom 4x4 matrix offset defining translation/orientation (16 numbers or path). Default=identity.")]
        [CommandOption("--CsysReferenceOffset <MATRIX>")]
        [DefaultValue("identity")]
        public string CsysReferenceOffset { get; set; } = "identity";

        // Coordinate System Reference Target
        [Description("Defines the reference object for 'WRT' (with respect to): owner, equi, sub_zone, zone, sub_site, or site. Default=owner.")]
        [CommandOption("--CsysWRT <TARGET>")]
        [DefaultValue("owner")]
        public string CsysWRT { get; set; } = "owner";

        [CommandOption("--save")]
        [Description("Optional file path to save the output. Used with json/csv/html formats.")]
        public string SavePath { get; set; }
    }


    public static class ProcessPhaseExtensions
    {
        /// <summary>
        /// Maps a string argument (case-insensitive) into a ProcessPhase enum.
        /// Returns Full if the input is null, empty, or unrecognized.
        /// </summary>
        public static ProcessPhase ParsePhase(string? phaseText)
        {
            if (string.IsNullOrWhiteSpace(phaseText))
                return ProcessPhase.Full;

            switch (phaseText.Trim().ToLowerInvariant())
            {
                case "parse": return ProcessPhase.Parse;
                case "validate": return ProcessPhase.Validate;
                case "plan": return ProcessPhase.Plan;
                case "execute": return ProcessPhase.Plan;
                case "full": return ProcessPhase.Full;
                default: return ProcessPhase.Full;
            }
        }
    }
    public static class ProcessCsysSettings
    {
        public static CsysOption GetCsysOption(string? userInput)
        {
            // Default Relative
            if (string.IsNullOrWhiteSpace(userInput))
                return CsysOption.Relative;

            switch (userInput.Trim().ToLowerInvariant())
            {
                case "absolute":
                case "abs":
                    return CsysOption.Absolute;

                case "global":
                case "glo":
                case "gbl":
                    return CsysOption.Global;

                case "relative":
                case "rel":
                    return CsysOption.Relative;

                case "transformed":
                case "transform":
                case "trans":
                case "tsf":
                    return CsysOption.Transformed;

                case "withoffset":
                case "offset":
                case "off":
                    return CsysOption.WithOffset;

                default:
                    // 🚨 Optional: could log to CLI — “Unknown option, using Relative”
                    return CsysOption.Relative;
            }
        }
        public static CsysReferenceOffset GetCsysReferenceOffset(string? userInput)
        {
            // 1️ Return identity/default offset if no input provided
            if (string.IsNullOrWhiteSpace(userInput) || userInput.ToLowerInvariant() == "identity")
                return new CsysReferenceOffset(); // Default constructor → identity matrix

            try
            {
                // 2️ Split by space, comma, or semicolon
                var tokens = userInput
                    .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToArray();

                // 3️ Expect 16 elements for a 4x4 matrix
                if (tokens.Length != 16)
                {
                    Console.WriteLine($"⚠️ Expected 16 numeric values, got {tokens.Length}. Using identity instead.");
                    return new CsysReferenceOffset();
                }

                // 4️ Parse to double[,] safely
                var matrix = new double[4, 4];
                int index = 0;

                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        if (double.TryParse(tokens[index], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var value))
                        {
                            matrix[i, j] = value;
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Invalid value '{tokens[index]}' at position {index}. Replaced with 0.");
                            matrix[i, j] = 0.0;
                        }

                        index++;
                    }
                }

                // 5️ Return built offset
                return new CsysReferenceOffset { Matrix4x4 = matrix };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to parse CSYS reference offset: {ex.Message}");
                return new CsysReferenceOffset(); // fallback
            }
        }
        public static CsysWRT GetCsysWRT(string? userInput)
        {
            // 🧭 Default to Owner
            if (string.IsNullOrWhiteSpace(userInput))
                return CsysWRT.Owner;

            switch (userInput.Trim().ToLowerInvariant())
            {
                case "owner":
                case "own":
                    return CsysWRT.Owner;

                case "equi":
                case "equipment":
                    return CsysWRT.EQUI;

                case "subzone":
                case "sub_zone":
                case "sub-zone":
                    return CsysWRT.SUB_ZONE;

                case "zone":
                    return CsysWRT.ZONE;

                case "subsite":
                case "sub_site":
                case "sub-site":
                    return CsysWRT.SUB_SITE;

                case "site":
                    return CsysWRT.SITE;

                default:
                    // 🚨 Optional: log warning, fallback to Owner
                    Console.WriteLine($"⚠️ Unknown WRT target '{userInput}', using default 'OWNER'.");
                    return CsysWRT.Owner;
            }
        }
    }
}
