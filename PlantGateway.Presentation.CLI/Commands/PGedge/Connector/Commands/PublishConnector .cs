using Microsoft.Extensions.Configuration;
using SMSgroup.Aveva.Application.CLI.PGedge.Connector.Templates;
using SMSgroup.Aveva.Application.CLI.Settings.Connector;
using SMSgroup.Aveva.Config.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Connector.Commands
{
    /// <summary>
    /// Publish connector launcher scripts (.cmd) for all configured connectors
    /// defined under the `Connectors` section in appsettings.
    /// Each connector gets its own .cmd file in its PublishFolder.
    /// </summary>
    [Description("Publish connector launcher scripts (.cmd) based on Connectors configuration.")]
    public sealed class PublishConnector : Command<PublishConnectorSettings>
    {
        private readonly IConfigProvider _configProvider;
        private readonly IConfiguration _configuration;

        public PublishConnector(IConfigProvider configProvider, IConfiguration configuration)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public override int Execute(CommandContext context, PublishConnectorSettings settings)
        {
            try
            {
                // ---------------------------------------------------------------
                // 1) LOAD CONNECTORS SECTION
                // ---------------------------------------------------------------
                var connectorsSection = _configuration.GetSection("Connectors");
                if (!connectorsSection.Exists())
                    throw new InvalidOperationException("Config section 'Connectors' is missing in appsettings.");

                var allConnectorSections = connectorsSection.GetChildren().ToList();
                if (allConnectorSections.Count == 0)
                    throw new InvalidOperationException("No connectors defined under 'Connectors' in appsettings.");

                // Optional filter: --connector <KEY>
                var filterKey = settings.ConnectorName?.Trim();
                var connectorsToProcess = string.IsNullOrWhiteSpace(filterKey)
                    ? allConnectorSections
                    : allConnectorSections
                        .Where(c => string.Equals(c.Key, filterKey, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (connectorsToProcess.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"No connectors matched key '{filterKey}'. Check the 'Connectors' section in appsettings.");
                }

                // ---------------------------------------------------------------
                // 2) RESOLVE LAUNCHER EXECUTABLE PATH (GLOBAL)
                // CLI override -> evar default path (from config)
                // ---------------------------------------------------------------
                var cliPath = settings.Target ?? _configProvider.GetPgedgeDesktopConnectorEvarValue();

                if (string.IsNullOrWhiteSpace(cliPath))
                    throw new Exception(
                        "CLI path cannot be empty. Provide --clipath or set EnvironmentVariables:PGEDGE_CLI_LAUNCHER in config.");

                cliPath = cliPath.Trim();

                // We don't force File.Exists(cliPath) here because in some environments
                // the .cmd may be generated on one machine and executed on another.
                // If you want strict validation, uncomment the check below.
                //
                // if (!File.Exists(cliPath))
                //     throw new FileNotFoundException($"CLI executable not found at: {cliPath}");

                // ---------------------------------------------------------------
                // 3) RESOLVE ENVIRONMENT VARIABLE NAME (GLOBAL)
                // CLI override -> config helper
                // ---------------------------------------------------------------
                var evarName =
                    settings.EvarName
                    ?? _configProvider.GetPgedgeDesktopConnectorEvarName();

                if (string.IsNullOrWhiteSpace(evarName))
                    throw new Exception(
                        "Environment variable name cannot be empty. Provide --evar or configure it in config.");

                evarName = evarName.Trim();

                // ---------------------------------------------------------------
                // 4) GENERATE SCRIPTS FOR EACH CONNECTOR
                // ---------------------------------------------------------------
                AnsiConsole.MarkupLine("[grey]Publishing connector launchers based on 'Connectors' configuration...[/]");

                foreach (var connectorSection in connectorsToProcess)
                {
                    var connectorKey = connectorSection.Key;

                    // Connector: display / logical name used in the script
                    var connectorName = connectorSection["Connector"];
                    if (string.IsNullOrWhiteSpace(connectorName))
                    {
                        connectorName = connectorKey; // fallback to key
                        AnsiConsole.MarkupLineInterpolated(
                            $"[yellow]⚠ Connector '{connectorKey}' has no 'Connector' value. Falling back to key as name.[/]");
                    }
                    connectorName = connectorName.Trim();

                    // Publish folder: per-connector, can be overridden via --publishto
                    var publishFolder =
                        settings.PublishDirectory
                        ?? connectorSection["PublishFolder"];

                    if (string.IsNullOrWhiteSpace(publishFolder))
                        throw new Exception(
                            $"Publish folder cannot be empty for connector '{connectorKey}'. " +
                            $"Provide --publishto or configure Connectors:{connectorKey}:PublishFolder.");

                    publishFolder = publishFolder.Trim();
                    Directory.CreateDirectory(publishFolder);

                    // Output file: <PublishFolder>\<ConnectorName>.cmd
                    var fileName = connectorName + ".cmd";
                    var outputPath = Path.Combine(publishFolder, fileName);

                    // Build script content using the shared template
                    var script = ConnectorScriptTemplate.Build(
                        inputCliPath: cliPath,
                        inputConnectorName: connectorName,
                        inputEvarName: evarName
                    );

                    File.WriteAllText(outputPath, script,
                        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                    AnsiConsole.MarkupLineInterpolated(
                        $"[green]✔ Connector script created:[/] [white]{outputPath}[/]");
                }

                AnsiConsole.MarkupLineInterpolated(
                    $"[grey]All scripts use environment variable:[/] [white]{evarName}[/]");
                AnsiConsole.MarkupLine("[grey]Double-click any connector .cmd to configure the launcher environment.[/]");

                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[red]❌ Connector publish failed:[/] {ex.Message}");
                return -1;
            }
        }

    }
}
