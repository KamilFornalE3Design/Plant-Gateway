using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;
using SMSgroup.Aveva.Utilities.Validator.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Execution.Validator.Rules.Attributes
{
    /// <summary>
    /// Geometry validation helper for 'geom' attributes.
    /// Uses the PipelineContract to resolve input folder.
    ///
    /// Rules:
    /// - 'geom' must be non-empty
    /// - must point to a .stp file
    /// - file must exist in:
    ///     - input folder (same as XML)
    ///     - inputFolder\STP
    ///     - inputFolder\STL
    ///
    /// If all checks pass, a single info message
    /// "Geometry validation passed." is emitted.
    /// </summary>
    internal static class AttributeRule_Geometry
    {
        public static void Validate(XAttribute geomAttribute, ValidatorResult result, PipelineContract<ProjectStructureDTO> contract)
        {
            if (geomAttribute == null)
                throw new ArgumentNullException(nameof(geomAttribute));
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (contract.Input == null)
                throw new ArgumentException("PipelineContract.Input cannot be null", nameof(contract));

            var location = BuildLocationInfo(geomAttribute);

            if (!EnsureValueNotEmpty(geomAttribute, result, location)) return;
            if (!EnsureStpExtension(geomAttribute, result, location)) return;
            if (!TryGetBaseFolder(contract, out var baseFolder, result, location)) return;
            if (!EnsureFileExists(geomAttribute, baseFolder, result, location)) return;

            // All checks passed
            AddInfo(result, $"Geometry validation passed for '{geomAttribute.Value}'.{location}");
        }

        // ─────────────────────────────────────────────────────────
        // Step helpers (each returns bool)
        // ─────────────────────────────────────────────────────────

        private static string BuildLocationInfo(XAttribute attribute)
        {
            var owner = attribute.Parent;
            if (owner is IXmlLineInfo li && li.HasLineInfo())
            {
                return $" (line {li.LineNumber}, position {li.LinePosition})";
            }

            return string.Empty;
        }

        private static bool EnsureValueNotEmpty(XAttribute attribute, ValidatorResult result, string locationInfo)
        {
            var value = attribute.Value?.Trim();

            if (!string.IsNullOrWhiteSpace(value))
                return true;

            AddError(result, $"Geometry attribute 'geom' is empty.{locationInfo}");
            return false;
        }

        private static bool EnsureStpExtension(XAttribute attribute, ValidatorResult result, string locationInfo)
        {
            var value = attribute.Value?.Trim() ?? string.Empty;
            var ext = Path.GetExtension(value);

            if (ext.Equals(".stp", StringComparison.OrdinalIgnoreCase))
                return true;

            AddError(result,
                $"Geometry attribute 'geom' must point to a .stp file (found '{value}').{locationInfo}");
            return false;
        }

        private static bool TryGetBaseFolder(
            PipelineContract<ProjectStructureDTO> contract,
            out string baseFolder,
            ValidatorResult result,
            string locationInfo)
        {
            var filePath = contract.Input.FilePath;

            baseFolder = string.IsNullOrWhiteSpace(filePath)
                ? string.Empty
                : Path.GetDirectoryName(filePath) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(baseFolder))
            {
                AddWarning(result,
                    $"Cannot verify geometry file because input folder could not be resolved from contract.{locationInfo}");
                return false;
            }

            if (!Directory.Exists(baseFolder))
            {
                AddWarning(result,
                    $"Cannot verify geometry file because input folder '{baseFolder}' does not exist.{locationInfo}");
                return false;
            }

            return true;
        }

        private static bool EnsureFileExists(XAttribute attribute, string baseFolder, ValidatorResult result, string locationInfo)
        {
            var rawValue = attribute.Value?.Trim() ?? string.Empty;
            var fileName = Path.GetFileName(rawValue);

            if (string.IsNullOrEmpty(fileName))
            {
                AddError(result,
                    $"Geometry attribute 'geom' does not contain a valid file name ('{rawValue}').{locationInfo}");
                return false;
            }

            var stpFolder = Path.Combine(baseFolder, "STP");
            var stlFolder = Path.Combine(baseFolder, "STL");

            var candidates = new[]
            {
                Path.Combine(baseFolder, fileName),
                Path.Combine(stpFolder, fileName),
                Path.Combine(stlFolder, fileName)
            };

            var exists = candidates.Any(File.Exists);

            if (exists)
                return true;

            AddError(result,
                $"Geometry file '{rawValue}' could not be found in '{baseFolder}' and the subfolders: '{stpFolder}' or '{stlFolder}'.{locationInfo}");
            return false;
        }


        // ─────────────────────────────────────────────────────────
        // Result helpers – adjust to your ValidatorResult shape
        // ─────────────────────────────────────────────────────────

        private static void AddError(ValidatorResult result, string message)
        {
            // If ValidatorResult has 'Error' list like ParserResult, use that instead.
            // Example: result.Error.Add(message);
            result.Errors.Add(message);
        }

        private static void AddWarning(ValidatorResult result, string message)
        {
            result.Warnings.Add(message);
        }

        private static void AddInfo(ValidatorResult result, string message)
        {
            // If you don't have a Messages/Infos collection yet, you can:
            //  - use Warnings for non-critical info, or
            //  - extend ValidatorResult with a Messages list.
            result.Message.Add(message);
        }
    }
}
