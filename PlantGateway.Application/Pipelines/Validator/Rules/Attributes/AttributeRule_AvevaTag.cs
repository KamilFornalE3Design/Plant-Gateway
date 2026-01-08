using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;
using System.Xml;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Validator.Rules.Attributes
{
    internal static class AttributeRule_AvevaTag //: IValidationRule<InputTarget>
    {
        public static void Validate(XAttribute avevaTagAttribute, ValidatorResult result, ISet<string> seenAvevaTags, PipelineContract<ProjectStructureDTO> contract)
        {
            if (avevaTagAttribute == null)
                throw new ArgumentNullException(nameof(avevaTagAttribute));
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (seenAvevaTags == null)
                throw new ArgumentNullException(nameof(seenAvevaTags));
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));

            // Build location info to append to messages with line/position
            var location = BuildLocationInfo(avevaTagAttribute);

            bool isValid = true;

            bool hasBaseValue = EnsureValueNotEmpty(avevaTagAttribute, result, location);
            if (!hasBaseValue)
            {
                isValid = false;
            }
            else
            {
                if (!EnsureUnique(avevaTagAttribute, result, seenAvevaTags, location))
                    isValid = false;
            }

            if (isValid)
            {
                AddInfo(
                    result,
                    $"AvevaTag validation passed for '{avevaTagAttribute.Value}'.{location}"
                );
            }

        }

        // ─────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────

        // This function should be moved to base, it is recurring in many rules. Do the same with AddError, AddWarning, AddInfo!
        private static string BuildLocationInfo(XAttribute attribute)
        {
            var owner = attribute.Parent;
            if (owner is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
            {
                return $" (line {lineInfo.LineNumber}, position {lineInfo.LinePosition})";
            }

            return string.Empty;
        }

        private static bool EnsureValueNotEmpty(XAttribute attribute, ValidatorResult result, string locationInfo)
        {
            var value = attribute.Value?.Trim();

            if (!string.IsNullOrWhiteSpace(value))
                return true;

            AddError(result, $"Attribute 'AvevaTag' is empty.{locationInfo}");
            return false;
        }

        private static bool EnsureUnique(XAttribute attribute, ValidatorResult result, ISet<string> seenAvevaTags, string locationInfo)
        {
            var value = attribute.Value?.Trim() ?? string.Empty;

            // If for some reason it's empty here, treat it as non-unique / invalid
            if (string.IsNullOrEmpty(value))
            {
                AddError(result, $"Attribute 'AvevaTag' is empty or invalid.{locationInfo}");
                return false;
            }

            if (!seenAvevaTags.Add(value))
            {
                // Value already present in set -> duplicate. Non-fatal, just warn.
                AddWarning(result, $"Duplicate name attribute value '{value}' detected in document.{locationInfo}");

                return true;
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────
        // Result helpers
        // ─────────────────────────────────────────────────────────

        private static void AddError(ValidatorResult result, string message)
        {
            result.Errors.Add(message);
        }

        private static void AddWarning(ValidatorResult result, string message)
        {
            result.Warnings.Add(message);
        }

        private static void AddInfo(ValidatorResult result, string message)
        {
            result.Message.Add(message);
        }
    }
}
