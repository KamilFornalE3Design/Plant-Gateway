using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Protocol.Template
{
    /// <summary>
    /// Builds install artifacts for the pgedge:// protocol.
    /// Now: USER-LEVEL ONLY, single registry entry, single verb.
    /// </summary>
    public static class ProtocolTemplate
    {
        /// <summary>
        /// Generates the full contents of a <c>.reg</c> file required to register a
        /// custom Windows URL protocol (e.g. <c>pgedge://</c>) at the
        /// <c>HKEY_CURRENT_USER\Software\Classes</c> registry root.
        /// <para>
        /// This method produces a user-level protocol registration that instructs
        /// Windows to launch the PGedge command-line application through a fixed
        /// environment variable (typically <c>PGEDGE_CLI_LAUNCHER</c>), ensuring that
        /// protocol resolution is portable and can be updated without modifying the registry.
        /// </para>
        /// <para>
        /// The resulting registry entry has the form:
        /// <code>
        /// "%PGEDGE_CLI_LAUNCHER%" "%1"
        /// </code>
        /// where <c>%1</c> represents the full URL invoked by Windows (e.g.
        /// <c>pgedge://cli/open</c>). The CLI receives this URL as its first argument
        /// and routes it via the PGedge protocol dispatcher.
        /// </para>
        /// </summary>
        /// <param name="scheme">
        /// The URL protocol scheme name, such as <c>"pgedge"</c>. This value determines
        /// the portion of the URL before <c>://</c>. For example, if the scheme is
        /// <c>"pgedge"</c>, Windows recognizes URLs in the form <c>pgedge://...</c>.
        /// </param>
        /// <param name="evarName">
        /// The name of the environment variable that resolves to the PGedge launcher
        /// executable (typically <c>PGEDGE_CLI_LAUNCHER</c>). This variable must be
        /// defined prior to protocol usage. If the variable is missing, Windows cannot
        /// resolve the executable path, and protocol activation will fail.
        /// </param>
        /// <returns>
        /// A string containing the complete <c>.reg</c> file content suitable for writing
        /// to disk and importing via <c>regedit.exe</c> or a PowerShell installation script.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="scheme"/> or <paramref name="evarName"/> is
        /// <see langword="null"/>, empty, or whitespace.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This function does not modify the registry directly. It only produces the
        /// textual artifact. Installation is typically performed by a generated
        /// <c>install-protocol.ps1</c> script created by <see cref="BuildPs1"/>.
        /// </para>
        /// <para>
        /// Windows URL protocols require only the <c>open</c> verb. Additional verbs are
        /// not supported for protocol handlers.
        /// </para>
        /// <para>
        /// The registration is intentionally user-level (<c>HKCU</c>) to avoid requiring
        /// administrative permissions and to prevent machine-level conflicts.
        /// </para>
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// var regContent = ProtocolTemplate.BuildReg("pgedge", "PGEDGE_CLI_LAUNCHER");
        /// File.WriteAllText("install-protocol.reg", regContent);
        /// </code>
        /// </example>

        public static string BuildReg(string scheme, string evarName)
        {
            if (string.IsNullOrWhiteSpace(scheme))
                throw new ArgumentException("Scheme must be provided.", nameof(scheme));

            if (string.IsNullOrWhiteSpace(evarName))
                throw new ArgumentException(
                    "Environment variable name must be provided. " +
                    "Protocol requires PGEDGE_CLI_LAUNCHER.",
                    nameof(evarName));

            var root = $@"HKEY_CURRENT_USER\Software\Classes\{scheme}";
            var sb = new StringBuilder();

            sb.AppendLine("Windows Registry Editor Version 5.00");
            sb.AppendLine();
            sb.AppendLine($@"[{root}]");
            sb.AppendLine($@"@=""{scheme.ToUpperInvariant()} URL Protocol""");
            sb.AppendLine(@"""URL Protocol""=""""");
            sb.AppendLine();
            sb.AppendLine($@"[{root}\shell]");
            sb.AppendLine(@"@=""open""");
            sb.AppendLine();
            sb.AppendLine($@"[{root}\shell\open]");
            sb.AppendLine($@"@=""Open with {scheme.ToUpperInvariant()} Launcher""");
            sb.AppendLine();
            sb.AppendLine($@"[{root}\shell\open\command]");

            sb.AppendLine($@"@=""""%{evarName}%"" ""%1""");

            return sb.ToString();
        }
        /// <summary>
        /// Generates a PowerShell installation script (<c>install-protocol.ps1</c>)
        /// responsible for installing the PGedge URL protocol handler at the
        /// user level (<c>HKCU</c>) and setting the launcher environment variable
        /// required by <see cref="BuildReg"/>.
        /// </summary>
        /// <param name="scheme">
        /// The URL protocol scheme to register (e.g. <c>"pgedge"</c>). This determines
        /// which scheme Windows associates with the generated registry entries.
        /// </param>
        /// <param name="evarName">
        /// The name of the environment variable that will store the absolute path
        /// to the PGedge launcher executable (typically <c>PGEDGE_CLI_LAUNCHER</c>).
        /// The script expects a <c>-CliPath</c> parameter to assign to this variable.
        /// </param>
        /// <returns>
        /// A fully-formed PowerShell script that:
        /// <list type="bullet">
        /// <item><description>
        /// Validates that the provided launcher path exists.
        /// </description></item>
        /// <item><description>
        /// Writes <paramref name="evarName"/> to the user's environment variables.
        /// </description></item>
        /// <item><description>
        /// Creates the protocol registry structure under
        /// <c>HKCU:\Software\Classes\{scheme}\shell\open\command</c>.
        /// </description></item>
        /// <item><description>
        /// Registers the protocol handler so that Windows executes:
        /// <code>
        /// "%PGEDGE_CLI_LAUNCHER%" "%1"
        /// </code>
        /// when a <c>pgedge://</c> URL is clicked.
        /// </description></item>
        /// </list>
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="scheme"/> or <paramref name="evarName"/> is
        /// <see langword="null"/>, empty, or whitespace.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The script produced by this function performs the actual registry
        /// installation, whereas <see cref="BuildReg"/> only generates the static
        /// registration file used for publishing installer artifacts.
        /// </para>
        /// <para>
        /// Installing the protocol at the user level (HKCU) avoids UAC elevation,
        /// supports multi-user environments, and conforms to modern application
        /// deployment practices.
        /// </para>
        /// <para>
        /// The launcher path (<c>-CliPath</c>) must point to the PGedge CLI
        /// executable that implements protocol dispatch logic. This script does not
        /// attempt to discover the executable automatically.
        /// </para>
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// var ps1 = ProtocolTemplate.BuildPs1("pgedge", "PGEDGE_CLI_LAUNCHER");
        /// File.WriteAllText("install-protocol.ps1", ps1);
        /// </code>
        /// </example>

        public static string BuildPs1(string scheme, string evarName)
        {
            if (string.IsNullOrWhiteSpace(scheme))
                throw new ArgumentException("Scheme must be provided.", nameof(scheme));

            if (string.IsNullOrWhiteSpace(evarName))
                throw new ArgumentException(
                    "Environment variable name must be provided. " +
                    "Protocol requires PGEDGE_CLI_LAUNCHER.",
                    nameof(evarName));

            return
        $@"param(
    [Parameter(Mandatory=$true)]
    [string]$CliPath
)

Write-Host ""Installing {scheme} protocol (USER level)..."" -ForegroundColor Cyan

if (-not (Test-Path $CliPath)) {{
    Write-Error ""Launcher path '$CliPath' does not exist.""
    exit 1
}}

# ----------------------------------------------------------------------
# Set explicit environment variable (USER level)
# ----------------------------------------------------------------------
[Environment]::SetEnvironmentVariable(""{evarName}"", $CliPath, 'User')
Write-Host ""Set user-level environment variable: {evarName}=$CliPath"" -ForegroundColor Green

# ----------------------------------------------------------------------
# Registry root (HKCU)
# ----------------------------------------------------------------------
$root = ""HKCU:\Software\Classes\{scheme}""

New-Item -Path $root -Force | Out-Null
Set-ItemProperty -Path $root -Name ""URL Protocol"" -Value """" 
Set-ItemProperty -Path $root -Name ""(default)"" -Value ""{scheme.ToUpperInvariant()} URL Protocol""

# ----------------------------------------------------------------------
# Command key (uses ONLY the environment variable)
# ----------------------------------------------------------------------
$cmdKey = Join-Path $root ""shell\open\command""
New-Item -Path $cmdKey -Force | Out-Null

$cmd = '""%{evarName}%"" ""%1""'
Set-ItemProperty -Path $cmdKey -Name ""(default)"" -Value $cmd

Write-Host ""Protocol installed (USER). Test with: {scheme}://cli/open"" -ForegroundColor Yellow
";
        }

        public static string BuildOpenCmd(string scheme, string target, string verb)
        {
            return
$@"@echo off
REM Launch PGedge protocol for testing
start """" ""{scheme}://{target}/{verb}""
";
        }
    }
}