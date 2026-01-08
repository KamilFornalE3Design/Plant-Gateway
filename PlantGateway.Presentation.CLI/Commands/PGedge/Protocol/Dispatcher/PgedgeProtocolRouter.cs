using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Protocol.Dispatcher
{
    /// <summary>
    /// Routes Windows protocol handler URLs of the form:
    ///   pgedge://cli/open
    ///   pgedge://cli/run?cmd=convert-top
    ///   pgedge://webapp/open
    /// </summary>
    public sealed class PgedgeProtocolRouter
    {
        private const string Scheme = "pgedge";

        /// <summary>
        /// Routes an incoming protocol URL to the correct action handler.
        /// </summary>
        public int Route(string urlString)
        {
            if (!urlString.StartsWith($"{Scheme}://", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[red]Invalid protocol: must start with pgedge://[/]");
                return -1;
            }

            try
            {
                var url = new Uri(urlString);
                string target = url.Host;                  // e.g. cli, webapp, acc
                string verb = url.AbsolutePath.Trim('/');  // e.g. open, run
                NameValueCollection query = HttpUtility.ParseQueryString(url.Query);

                return target.ToLowerInvariant() switch
                {
                    "cli" => HandleCli(verb, query),
                    "webapp" => HandleWebApp(verb),
                    "acc" => HandleAcc(verb),
                    _ => UnknownTarget(target)
                };
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]Protocol routing error:[/] {ex.Message}");
                return -1;
            }
        }

        // ----------------------------------------------------------------------
        // CLI target
        // ----------------------------------------------------------------------
        private int HandleCli(string verb, NameValueCollection q)
        {
            switch (verb.ToLowerInvariant())
            {
                case "open": return OpenCli();
                case "run": return RunCliCommand(q);
                default: return UnknownVerb(verb, "CLI");
            }
        }

        private static int OpenCli()
        {
            string exe = System.Environment.ProcessPath ?? "PlantGateway.CLI.exe";
            AnsiConsole.MarkupLine("[green]Opening PGedge CLI...[/]");

            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true
            });

            return 0;
        }

        private static int RunCliCommand(NameValueCollection q)
        {
            var command = q["cmd"];
            if (string.IsNullOrWhiteSpace(command))
            {
                AnsiConsole.MarkupLine("[red]Missing command: use ?cmd=...[/]");
                return -1;
            }

            string exe = System.Environment.ProcessPath ?? "PlantGateway.CLI.exe";
            AnsiConsole.MarkupLineInterpolated($"[green]Executing CLI command:[/] {command}");

            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = command,
                UseShellExecute = true
            });

            return 0;
        }

        // ----------------------------------------------------------------------
        // WebApp target
        // ----------------------------------------------------------------------
        private static int HandleWebApp(string verb)
        {
            return verb.ToLowerInvariant() switch
            {
                "open" => OpenWebApp(),
                _ => UnknownVerb(verb, "WebApp")
            };
        }

        private static int OpenWebApp()
        {
            const string localUrl = "https://localhost:4400";

            AnsiConsole.MarkupLineInterpolated($"[green]Opening WebApp:[/] {localUrl}");

            Process.Start(new ProcessStartInfo
            {
                FileName = localUrl,
                UseShellExecute = true
            });

            return 0;
        }

        // ----------------------------------------------------------------------
        // ACC target (optional future)
        // ----------------------------------------------------------------------
        private static int HandleAcc(string verb)
        {
            return verb.ToLowerInvariant() switch
            {
                "open" => OpenAcc(),
                _ => UnknownVerb(verb, "ACC")
            };
        }

        private static int OpenAcc()
        {
            AnsiConsole.MarkupLine("[green]ACC protocol not yet implemented.[/]");
            return 0;
        }

        // ----------------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------------
        private static int UnknownTarget(string target)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Unknown protocol target:[/] {target}");
            return -1;
        }

        private static int UnknownVerb(string verb, string target)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Unknown {target} verb:[/] {verb}");
            return -1;
        }
    }
}
