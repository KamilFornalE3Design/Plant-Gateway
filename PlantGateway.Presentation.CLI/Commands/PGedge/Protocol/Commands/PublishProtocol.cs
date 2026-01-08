using Microsoft.Extensions.Configuration;
using SMSgroup.Aveva.Application.CLI.PGedge.Protocol.Template;
using SMSgroup.Aveva.Application.CLI.Settings.Protocol;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Protocol.Commands
{
    [Description("Publish URL protocol installer artifacts (reg + ps1 + cmd) for a PGedge connector.")]
    public sealed class PublishProtocol : Command<PublishProtocolSettings>
    {
        private readonly IConfiguration _config;

        public PublishProtocol(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public override int Execute(CommandContext context, PublishProtocolSettings settings)
        {
            try
            {
                // ---------------------------------------------------------
                // 1. Resolve Connector
                // ---------------------------------------------------------
                var connectorKey = settings.ConnectorKey;
                if (string.IsNullOrWhiteSpace(connectorKey))
                    connectorKey = "PgedgeCli";

                var connectorSection = _config.GetSection($"Connectors:{connectorKey}");
                if (!connectorSection.Exists())
                    throw new InvalidOperationException($"Connector config not found: Connectors:{connectorKey}");

                var protocolKey = connectorSection["ProtocolKey"];
                if (string.IsNullOrWhiteSpace(protocolKey))
                    throw new InvalidOperationException($"Connector '{connectorKey}' is missing ProtocolKey.");

                // ---------------------------------------------------------
                // 2. Resolve Protocol settings
                // ---------------------------------------------------------
                var protocolSection = _config.GetSection($"Protocols:{protocolKey}");
                if (!protocolSection.Exists())
                    throw new InvalidOperationException($"Protocol config not found: Protocols:{protocolKey}");

                var scheme = protocolSection["Scheme"] ?? "pgedge";
                var target = protocolSection["Target"] ?? "cli";
                var openVerb = protocolSection["OpenVerb"] ?? "open";

                // ---------------------------------------------------------
                // 3. Resolve Environment Variable name + value
                // ---------------------------------------------------------
                const string evarName = "PGEDGE_CLI_LAUNCHER";

                var launcherValue = _config[$"EnvironmentVariables:{evarName}"];
                if (string.IsNullOrWhiteSpace(launcherValue))
                    throw new InvalidOperationException(
                        $"Environment variable '{evarName}' is not defined in configuration: EnvironmentVariables:{evarName}");

                if (!File.Exists(launcherValue))
                    throw new FileNotFoundException(
                        $"Invalid PGEDGE_CLI_LAUNCHER path in config: {launcherValue}");

                // ---------------------------------------------------------
                // 4. Publish folder
                // ---------------------------------------------------------
                var publishFolder =
                    settings.PublishDirectory ??
                    connectorSection["PublishFolder"];

                if (string.IsNullOrWhiteSpace(publishFolder))
                    throw new Exception("Publish folder cannot be empty. Provide --publishto or configure Connectors:<ConnectorKey>:PublishFolder.");

                publishFolder = publishFolder.Trim();
                Directory.CreateDirectory(publishFolder);

                // ---------------------------------------------------------
                // 5. Build user-level artifacts
                // ---------------------------------------------------------
                var regContent = ProtocolTemplate.BuildReg(scheme, evarName);
                var ps1Content = ProtocolTemplate.BuildPs1(scheme, evarName);
                var cmdContent = ProtocolTemplate.BuildOpenCmd(scheme, target, openVerb);

                // ---------------------------------------------------------
                // 6. Write files
                // ---------------------------------------------------------
                var regPath = Path.Combine(publishFolder, "install-protocol.reg");
                var ps1Path = Path.Combine(publishFolder, "install-protocol.ps1");
                var cmdPath = Path.Combine(publishFolder, "pgedge-open.cmd");

                File.WriteAllText(regPath, regContent, new UTF8Encoding(false));
                File.WriteAllText(ps1Path, ps1Content, new UTF8Encoding(false));
                File.WriteAllText(cmdPath, cmdContent, new UTF8Encoding(false));

                // ---------------------------------------------------------
                // 7. Summary
                // ---------------------------------------------------------
                AnsiConsole.MarkupLine("[green]✔ Protocol artifacts created (user-level semantics):[/]");
                AnsiConsole.MarkupLine($"  [white]{regPath}[/]");
                AnsiConsole.MarkupLine($"  [white]{ps1Path}[/]");
                AnsiConsole.MarkupLine($"  [white]{cmdPath}[/]");

                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[grey]To install (user-level):[/]");
                AnsiConsole.MarkupLine("  [yellow]PowerShell > .\\install-protocol.ps1 -CliPath \"<PATH_TO_CLI>\"[/]");
                AnsiConsole.MarkupLine("[grey]Then test with:[/]");
                AnsiConsole.MarkupLine($"  [yellow]{scheme}://{target}/{openVerb}[/]");

                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]❌ Protocol publish failed:[/] {ex.Message}");
                return -1;
            }
        }
    }

}
