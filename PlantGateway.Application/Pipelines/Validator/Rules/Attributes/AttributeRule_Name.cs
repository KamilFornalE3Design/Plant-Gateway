using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Validator.Rules.Attributes
{
    /// <summary>
    /// Validates 'name' attributes:
    /// - must be non-empty
    /// - should be unique within the current document (case-insensitive)
    ///
    /// If all checks pass, a single info message
    /// "Name validation passed." is emitted.
    /// </summary>
    internal static class AttributeRule_Name
    {
        /// <summary>
        /// Validates a single 'name' attribute using a shared name registry
        /// for uniqueness within the document.
        /// </summary>
        public static void Validate(XAttribute nameAttribute, ValidatorResult result, ISet<string> seenNames)
        {
            if (nameAttribute == null)
                throw new ArgumentNullException(nameof(nameAttribute));
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (seenNames == null)
                throw new ArgumentNullException(nameof(seenNames));

            var location = BuildLocationInfo(nameAttribute);

            if (!EnsureValueNotEmpty(nameAttribute, result, location)) return;
            if (!EnsureUnique(nameAttribute, result, seenNames, location)) return;

            AddInfo(result, $"Name validation passed for '{nameAttribute.Value}'.{location}");
        }

        // ─────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────

        // This function should be moved to base, it is recurring in many rules. Do the same with AddError, AddWarning, AddInfo!
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

            AddError(result, $"Attribute 'Name' is empty.{locationInfo}");
            return false;
        }

        private static bool EnsureUnique(XAttribute attribute, ValidatorResult result, ISet<string> seenNames, string locationInfo)
        {
            var value = attribute.Value?.Trim() ?? string.Empty;

            // If for some reason it's empty here, treat it as non-unique / invalid
            if (string.IsNullOrEmpty(value))
            {
                AddError(result, $"Name attribute 'name' is empty or invalid.{locationInfo}");
                return false;
            }

            if (!seenNames.Add(value))
            {
                // Value already present in set -> duplicate
                AddWarning(result,
                    $"Duplicate name attribute value '{value}' detected in document.{locationInfo}");
                // Not a hard error (for now); we allow pipeline to continue.
                // If this should be fatal later, change AddWarning to AddError.
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
