using Spectre.Console.Cli;
using System.ComponentModel;

namespace PlantGateway.Presentation.CLI.Settings.PGedge.Environment
{

    /// <summary>
    /// Single entrypoint for environment control:
    ///   pgedge envctl <option> [--env <dev|stage|prod>] ...
    /// Examples:
    ///   pgedge envctl validate --env dev --verbosity verbose --log-target console
    ///   pgedge envctl set --name PG_ENV --value stage
    ///   pgedge envctl get --name PG_ENV
    ///   pgedge envctl get --all
    /// </summary>
    public sealed class EnvControlSettings : CommandSettings
    {
        // ── Operation option (default = Validate) ─────────────
        [CommandOption("--option <validate|set|get>")]
        [Description("Operation to perform. Defaults to 'validate'.")]
        public string? OptionText { get; init; }

        public EnvControlOpt Option => string.IsNullOrWhiteSpace(OptionText) ? EnvControlOpt.Val : EnvControlOptHelper.ResolveOption(OptionText);

        // ── Environment selection ─────────────────────────────
        [CommandOption("--env <ENV>")]
        [Description("Target environment (dev|stage|prod). Empty => auto dev/prod by Debugger.")]
        public string? Env { get; init; }

        // ── For 'set' ─────────────────────────────────────────
        [CommandOption("--name <NAME>")]
        [Description("ConfigEvars field to set (only 'environment' is allowed).")]
        public string? Name { get; init; }

        [CommandOption("--value <VALUE>")]
        [Description("Value to assign when using 'set'.")]
        public string? Value { get; init; }

        // ── For 'get' ─────────────────────────────────────────
        [CommandOption("--all")]
        [Description("With 'get': list all ConfigEvars fields.")]
        public bool All { get; init; }

        // ── Logging toggles ───────────────────────────────────
        [CommandOption("--verbose")]
        [Description("If set, logs all messages. If not set, logs only final verdict.")]
        public bool IsVerbose { get; init; }

        [CommandOption("--console")]
        [Description("If set, logs to console. If not set, logs to a file in the user profile.")]
        public bool IsLogConsole { get; init; }

        // Fixed log file path when logging to file
        public string LogFilePath
            => Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "pgedge_envctl.log");
    }


    public enum EnvControlOpt
    {
        Val,    // run schema + startup checks
        Set,    // set a given environment variable (e.g., PG_ENV) to a value
        Get,    // get a given environment variable (or --all)
        Rem     // rem a given environment variable (or --all)
    }

    public static class EnvControlOptHelper
    {
        /// <summary>
        /// Converts a user-provided text option to EnvControlOpt (case-insensitive).
        /// Supported aliases:
        ///   - Validate: "validate","val","check","verify"
        ///   - Set:      "set","change","switch"
        ///   - Get:      "get","current","show","print","list"
        /// Returns Unknown on null/empty/unrecognized.
        /// </summary>
        public static EnvControlOpt ResolveOption(string? option)
        {
            // Fail fast and Default set to Validate
            if (string.IsNullOrWhiteSpace(option))
                return EnvControlOpt.Val;

            var o = option.Trim().ToLowerInvariant();
            return o switch
            {
                "validate" or "val" or "check" or "verify" => EnvControlOpt.Val,
                "set" or "change" or "switch" => EnvControlOpt.Set,
                "get" or "current" or "show" or "print" or "list" => EnvControlOpt.Get,
                "rem" or "remove" or "delete" or "clear" => EnvControlOpt.Rem,
                _ => EnvControlOpt.Val
            };
        }
    }

    public enum LogVerbosity
    {
        Quiet,     // final verdict only
        Verbose    // full details
    }

    public static class LogVerbosityHelper
    {
        /// <summary>
        /// Resolve verbosity using a text value or boolean flags.
        /// Priority: explicit text > flags. Throws if both flags set.
        /// Defaults to Verbose if nothing specified.
        /// </summary>
        public static LogVerbosity Resolve(string? text, bool verboseFlag, bool quietFlag)
        {
            if (verboseFlag && quietFlag)
                throw new ArgumentException("Cannot specify both --verbose and --quiet.");

            if (!string.IsNullOrWhiteSpace(text))
            {
                var v = text.Trim().ToLowerInvariant();
                return v switch
                {
                    "quiet" or "q" => LogVerbosity.Quiet,
                    "verbose" or "v" => LogVerbosity.Verbose,
                    _ => throw new ArgumentException("Verbosity must be 'quiet' or 'verbose'.")
                };
            }

            if (quietFlag) return LogVerbosity.Quiet;
            if (verboseFlag) return LogVerbosity.Verbose;

            return LogVerbosity.Verbose; // sensible default
        }
    }

    public enum LogSink
    {
        Console, // write to console
        File     // write to a file (requires --log-file)
    }
}
