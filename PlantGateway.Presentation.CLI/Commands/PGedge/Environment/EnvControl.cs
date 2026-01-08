using Newtonsoft.Json.Linq;
using SMSgroup.Aveva.Application.CLI.Settings.Environment;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Implementation;
using SMSgroup.Aveva.Config.Models;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using SMSgroup.Aveva.Config.Validation;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Environment
{
    [Description("Validate shared config and DLLs for a specific environment.")]
    public sealed class EnvControl : Command<EnvControlSettings>
    {
        private readonly IConfigProvider _configProvider;

        public EnvControl(IConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public override int Execute(CommandContext context, EnvControlSettings settings)
        {
            var effective = EnvControlOptHelper.ResolveOption(context.Name);

            try
            {
                switch (effective)
                {
                    case EnvControlOpt.Val:
                        return RunValidate(settings);

                    case EnvControlOpt.Set:
                        return RunSet(settings);

                    case EnvControlOpt.Get:
                        return RunGet(settings);

                    case EnvControlOpt.Rem:
                        return RunRemove(settings);

                    default:
                        LogError(settings, "[red]Unknown option. Use: validate | set | get | remove[/]");
                        FlushIfFile(settings);
                        return 999; // show help
                }
            }
            catch (Exception ex)
            {
                LogError(settings, $"[red]❌ {ex.Message}[/]");
                FlushIfFile(settings);
                return 1;
            }
        }

        // -------------------------------------------------------------------------
        // Calls will be filled in next step:
        private int RunValidate(EnvControlSettings s)
        {
            try
            {
                var appEnv = ResolveEnv(s.Env);
                var evars = _configProvider.ResolveAppSettings(appEnv);

                LogSuccess(s, $"[bold]Validating environment: {evars.EnvironmentName}[/]");
                WriteLine(s, $"AppSettingsPath: {evars.AppSettingsPath}");
                WriteLine(s, $"SchemaPath:     {evars.SchemaPath}");

                var errors = new List<string>();
                var successes = new List<string>();

                // 🔹 1) Schema validation
                try
                {
                    var schemaValidator = new SchemaValidator();
                    if (!schemaValidator.IsValid(evars.AppSettingsPath, evars.SchemaPath))
                        errors.AddRange(schemaValidator.ErrorList);
                    else
                        successes.AddRange(schemaValidator.SuccessList);
                }
                catch (Exception ex)
                {
                    errors.Add("Schema validation error: " + ex.Message);
                }

                // 🔹 2) Startup validation
                try
                {
                    var startupValidator = new StartupValidator();
                    startupValidator.EnableLogging();
                    var report = startupValidator.Validate(_configProvider, evars.EnvironmentName);

                    if (!string.IsNullOrWhiteSpace(report))
                    {
                        var lines = report.Split('\n')
                                          .Select(x => x.Trim())
                                          .Where(x => !string.IsNullOrWhiteSpace(x));
                        successes.AddRange(lines);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add("Startup validation error: " + ex.Message);
                }

                // 🔹 3) Output results
                if (errors.Count == 0)
                {
                    LogSuccess(s, "[green]✅ Validation passed.[/]");
                    if (s.IsVerbose && successes.Count > 0)
                    {
                        WriteLine(s, "[green bold]✔ Details:[/]");
                        foreach (var msg in successes)
                            WriteLine(s, msg);
                    }

                    FlushIfFile(s);
                    return 0;
                }

                LogError(s, $"[red]❌ Validation failed with {errors.Count} issue(s).[/]");
                foreach (var err in errors)
                    LogError(s, "- " + err);

                FlushIfFile(s);
                return 1;
            }
            catch (Exception ex)
            {
                LogError(s, $"[red]❌ Fatal validation error: {ex.Message}[/]");
                FlushIfFile(s);
                return 1;
            }
        }

        private int RunSet(EnvControlSettings settings)
        {
            // ----------------------------------------
            // 1. Validate the name
            // ----------------------------------------
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                AnsiConsole.MarkupLine("[red]Environment variable name cannot be empty.[/]");
                return -1;
            }

            // ----------------------------------------
            // 2. Normalize values
            // ----------------------------------------
            var name = settings.Name.Trim();
            var value = string.IsNullOrWhiteSpace(settings.Value) ? null : settings.Value.Trim();

            // ----------------------------------------
            // 3. Attempt MACHINE-level write
            // ----------------------------------------
            try
            {
                System.Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Machine);

                AnsiConsole.MarkupLine($"[green] MACHINE:[/] {name} = [yellow]{value ?? "(deleted)"}[/]");

                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                // No admin rights → continue to USER fallback
                AnsiConsole.MarkupLine($"[darkorange3] No admin rights. Falling back to USER-level registry.[/]");
            }
            catch (Exception ex)
            {
                // MACHINE-level failed in an unexpected way (still ORANGE)
                AnsiConsole.MarkupLine($"[darkorange3] MACHINE write failed:[/] {ex.Message}");
            }

            // ----------------------------------------
            // 4. USER-level fallback
            // ----------------------------------------
            try
            {
                System.Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);

                AnsiConsole.MarkupLine($"[green] USER (fallback):[/] {name} = [yellow]{value ?? "(deleted)"}[/]");

                return 0;
            }
            catch (Exception ex)
            {
                // USER-level write failed → this is a real error
                AnsiConsole.MarkupLine($"[red] USER write failed:[/] {ex.Message}");
                return -1;
            }
        }

        private int RunGet(EnvControlSettings s)
        {
            try
            {
                var name = s.Name?.Trim();

                // ── 0) PGEDGE_CLI_LAUNCHER (User + Machine) ─────────────────────
                if (string.IsNullOrEmpty(name) || name.Equals("PGEDGE_CLI_LAUNCHER", StringComparison.OrdinalIgnoreCase))
                {
                    var userVal = System.Environment.GetEnvironmentVariable("PGEDGE_CLI_LAUNCHER", EnvironmentVariableTarget.User);
                    var machineVal = System.Environment.GetEnvironmentVariable("PGEDGE_CLI_LAUNCHER", EnvironmentVariableTarget.Machine);

                    if (string.IsNullOrEmpty(userVal) && string.IsNullOrEmpty(machineVal))
                    {
                        AnsiConsole.MarkupLine("[red bold]PGEDGE_CLI_LAUNCHER[/]: [yellow]⚠ not defined (User or Machine)[/]");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(userVal))
                            AnsiConsole.MarkupLine($"[green bold]PGEDGE_CLI_LAUNCHER (User)[/]: [white]{userVal}[/]");

                        if (!string.IsNullOrEmpty(machineVal))
                            AnsiConsole.MarkupLine($"[green bold]PGEDGE_CLI_LAUNCHER (Machine)[/]: [white]{machineVal}[/]");
                    }

                    if (!string.IsNullOrEmpty(name))
                        return 0;
                }

                // ── 1) OS Environment variables ────────────────────────────────
                if (string.IsNullOrEmpty(name) || name.Equals("PGEDGE_CONFIG_PATH", StringComparison.OrdinalIgnoreCase))
                {
                    var val = System.Environment.GetEnvironmentVariable("PGEDGE_CONFIG_PATH", EnvironmentVariableTarget.User);
                    if (string.IsNullOrEmpty(val))
                        AnsiConsole.MarkupLine("[red bold]PGEDGE_CONFIG_PATH[/]: [yellow]⚠ not defined[/]");
                    else
                        AnsiConsole.MarkupLine($"[green bold]PGEDGE_CONFIG_PATH[/]: [white]{val}[/]");

                    if (!string.IsNullOrEmpty(name)) return 0;
                }

                if (string.IsNullOrEmpty(name) || name.Equals("PGEDGE_ENV", StringComparison.OrdinalIgnoreCase))
                {
                    var val = System.Environment.GetEnvironmentVariable("PGEDGE_ENV", EnvironmentVariableTarget.User);
                    if (string.IsNullOrEmpty(val))
                        AnsiConsole.MarkupLine("[red bold]PGEDGE_ENV[/]: [yellow]⚠ not defined[/]");
                    else
                        AnsiConsole.MarkupLine($"[green bold]PGEDGE_ENV[/]: [white]{val}[/]");

                    if (!string.IsNullOrEmpty(name)) return 0;
                }

                // ── 2) ConfigEvars snapshot ───────────────────────────────────
                var appEnv = ResolveEnv(s.Env);
                var evars = _configProvider.ResolveAppSettings(appEnv);

                AnsiConsole.MarkupLine($"\n[bold underline]ConfigEvars snapshot for environment:[/] [aqua]{evars.EnvironmentName}[/]");

                // 🔹 ConfigPaths (base directories)
                if (evars.ConfigPaths.Count > 0)
                {
                    AnsiConsole.MarkupLine("\n[yellow bold]ConfigPaths (base directories):[/]");
                    foreach (var kv in evars.ConfigPaths)
                        AnsiConsole.MarkupLine($"  [cyan]{kv.Key,-20}[/] : [white]{kv.Value}[/]");
                }

                // 🔹 FileGroups (relative names)
                if (evars.FileGroups.Count > 0)
                {
                    AnsiConsole.MarkupLine("\n[yellow bold]FileGroups (relative file names):[/]");
                    foreach (var group in evars.FileGroups)
                    {
                        AnsiConsole.MarkupLine($"  [cyan]{group.Key}[/]:");
                        foreach (var kv in group.Value)
                            AnsiConsole.MarkupLine($"    [cyan]{kv.Key,-15}[/] : [white]{kv.Value}[/]");
                    }
                }

                // 🔹 ResolvedPaths (absolute, ready-to-use)
                if (evars.ResolvedPaths.Count > 0)
                {
                    AnsiConsole.MarkupLine("\n[yellow bold]ResolvedPaths (absolute paths):[/]");
                    foreach (var kv in evars.ResolvedPaths)
                        AnsiConsole.MarkupLine($"  [cyan]{kv.Key,-20}[/] : [white]{kv.Value}[/]");
                }

                FlushIfFile(s);
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red bold]❌ Failed to resolve config:[/] {ex.Message}");
                FlushIfFile(s);
                return 1;
            }
        }

        private int RunRemove(EnvControlSettings s)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(s.Name))
                {
                    AnsiConsole.MarkupLine("[red]Missing --name for 'rem'.[/]");
                    FlushIfFile(s);
                    return 1;
                }

                var name = s.Name.Trim();

                if (name.Equals("PGEDGE_CONFIG_PATH", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("PGEDGE_ENV", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("PGEDGE_CLI_LAUNCHER", StringComparison.OrdinalIgnoreCase))
                {
                    System.Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.User);
                    System.Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.Machine);

                    AnsiConsole.MarkupLine($"[green]✅ {name} removed (User + Machine scope).[/]");
                    FlushIfFile(s);
                    return 0;
                }


                AnsiConsole.MarkupLine($"[red]{s.Name}[/] is not removable. Supported: 'PGEDGE_CONFIG_PATH', 'PGEDGE_ENV'.");
                FlushIfFile(s);
                return 1;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error in 'rem': {ex.Message}[/]");
                FlushIfFile(s);
                return 1;
            }
        }


        #region Helpers (logging, env resolution, field mapping)

        private static string StripMarkup(string text)
            => Regex.Replace(text ?? string.Empty, @"\[[^\]]+\]", string.Empty);

        // One buffer shared during execution for file logging
        private static readonly List<string> LogLines = new();

        private static void WriteLine(EnvControlSettings s, string markup)
        {
            if (!s.IsVerbose) return;

            if (s.IsLogConsole)
                AnsiConsole.MarkupLine(markup);
            else
                LogLines.Add(StripMarkup(markup));
        }

        private static void LogSuccess(EnvControlSettings s, string markup)
        {
            if (s.IsLogConsole)
                AnsiConsole.MarkupLine(markup);
            else
                LogLines.Add(StripMarkup(markup));
        }

        private static void LogError(EnvControlSettings s, string markup)
        {
            if (s.IsLogConsole)
                AnsiConsole.MarkupLine(markup);
            else
                LogLines.Add(StripMarkup(markup));
        }
        private static void DumpObject(EnvControlSettings s, object obj, int indentLevel)
        {
            if (obj == null)
            {
                WriteLine(s, new string(' ', indentLevel * 2) + "[grey](null)[/]");
                return;
            }

            var type = obj.GetType();

            // Scalars / strings
            if (type.IsPrimitive || obj is string || obj is decimal)
            {
                LogSuccess(s, new string(' ', indentLevel * 2) + obj.ToString());
                return;
            }

            // Enumerables (if any later)
            if (obj is System.Collections.IEnumerable enumerable && !(obj is string))
            {
                foreach (var item in enumerable)
                    DumpObject(s, item, indentLevel + 1);
                return;
            }

            // Complex object → iterate properties
            foreach (var prop in type.GetProperties())
            {
                var value = prop.GetValue(obj);
                var prefix = new string(' ', indentLevel * 2);

                if (value == null)
                {
                    WriteLine(s, $"{prefix}{prop.Name}: [grey](null)[/]");
                }
                else if (prop.PropertyType.IsPrimitive || value is string || value is decimal)
                {
                    LogSuccess(s, $"{prefix}{prop.Name}: {value}");
                }
                else
                {
                    WriteLine(s, $"{prefix}{prop.Name}:");
                    DumpObject(s, value, indentLevel + 1);
                }
            }
        }

        private static void FlushIfFile(EnvControlSettings s)
        {
            if (s.IsLogConsole) return;

            var path = s.LogFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllLines(path, LogLines);

            AnsiConsole.MarkupLineInterpolated($"[grey]📝 Log written to:[/] {path}");
        }

        // Environment resolution helper
        private static AppEnvironment ResolveEnv(string? raw)
            => ConfigProvider.ResolveAppEnv(raw ?? string.Empty);

        // ConfigEvars field resolver (aliases for friendly names)
        private static bool TryGetConfigEvarValue(ConfigEvars e, string name, out string value, out string normalizedName)
        {
            value = string.Empty;
            normalizedName = name;

            switch (name.Trim().ToLowerInvariant())
            {
                case "env":
                case "environment":
                case "environmentname":
                    value = e.EnvironmentName ?? string.Empty;
                    normalizedName = "EnvironmentName";
                    return true;

                case "appsettings":
                case "appsettingspath":
                case "settings":
                case "settingspath":
                    value = e.AppSettingsPath ?? string.Empty;
                    normalizedName = "AppSettingsPath";
                    return true;

                case "schema":
                case "schemapath":
                    value = e.SchemaPath ?? string.Empty;
                    normalizedName = "SchemaPath";
                    return true;

                default:
                    return false;
            }
        }

        #endregion

    }
}
