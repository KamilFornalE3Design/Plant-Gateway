using SMSgroup.Aveva.Config.Attributes;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using SMSgroup.Aveva.Config.Models.ExecutionStepResult;
using SMSgroup.Aveva.Config.Models.Extensions;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Parser.Analyzers.Attributes
{
    /// <summary>
    /// Dedicated surface analyzer for a single AvevaTag value.
    /// Performs shallow checks only (existence, emptiness, separator presence).
    /// No semantic parsing, no codification, no validation.
    /// </summary>
    public sealed class AvevaTag //: IAnalyzer<InputTarget>
    {
        // ─────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────

        #region Public API

        // generic

        public ParserStepResult Analyze(object untypedObject, ParserResult parserResult)
        {
            if (untypedObject is InputTarget inputTarget)
                return Analyze(inputTarget, parserResult);

            throw new NotSupportedException("AvevaTag analyzer only supports InputTarget at this time.");
        }

        public ParserStepResult Analyze(InputTarget inputTarget, ParserResult parserResult)
        {
            var result = new ParserStepResult();

            return result;
        }

        // XML-specific
        public ParserStepResult Analyze(XElement xElement, ParserResult parserResult)
        {
            var result = new ParserStepResult
            {
                StepName = "AvevaTagSurfaceAnalyzer(XML)",
                Summary = "Surface-level checks for AvevaTag attribute from XML."
            };

            if (xElement is null)
            {
                result.AddError("XElement is NULL.");
                return result;
            }

            // Normalized lookup
            var xAttribute = GetAttributeNormalized(xElement, PlantGatewayAttribute.AvevaTag);
            var tag = xAttribute?.Value ?? string.Empty;

            // Run checks
            var exists = CheckExists(tag, result);
            var notEmpty = CheckNotEmpty(tag, result);
            var separator = CheckSeparator(tag, result);
            var duplicate = CheckDuplicate(tag, result);

            // Store rich results (metadata + boolean)
            result.Data[nameof(ParserClassificationCode.CheckExists)] =
                ParserClassificationCode.CheckExists.ToResult(exists);

            result.Data[nameof(ParserClassificationCode.CheckNotEmpty)] =
                ParserClassificationCode.CheckNotEmpty.ToResult(notEmpty);

            result.Data[nameof(ParserClassificationCode.CheckSeparator)] =
                ParserClassificationCode.CheckSeparator.ToResult(separator);

            result.Data[nameof(ParserClassificationCode.CheckDuplicate)] =
                ParserClassificationCode.CheckDuplicate.ToResult(duplicate);

            return result;
        }




        #endregion

        // ─────────────────────────────────────────────────────────────
        // Surface Checks
        // ─────────────────────────────────────────────────────────────

        #region Surface Checks

        /// <summary>
        /// Checks if the AvevaTag attribute was provided at all (null vs non-null).
        /// </summary>
        private bool CheckExists(string? avevaTag, ParserStepResult stepResult)
        {
            if (avevaTag == null)
            {
                stepResult.AddError("AvevaTag is NULL.");
                return false;
            }

            stepResult.AddInfo("AvevaTag exists.");
            return true;
        }


        /// <summary>
        /// Checks if the attribute contains non-whitespace content.
        /// </summary>
        private bool CheckNotEmpty(string? avevaTag, ParserStepResult stepResult)
        {
            if (string.IsNullOrWhiteSpace(avevaTag))
            {
                stepResult.AddWarning("AvevaTag is empty or whitespace.");
                return false;
            }

            stepResult.AddInfo("AvevaTag is not empty.");
            return true;
        }


        /// <summary>
        /// Checks if the tag contains the expected separator characters: '.' for segments.
        /// </summary>
        private bool CheckSeparator(string? avevaTag, ParserStepResult stepResult)
        {
            if (avevaTag == null)
                return false;

            bool hasDot = avevaTag.Contains('.');

            if (!hasDot)
            {
                stepResult.AddWarning("AvevaTag does not contain any '.' segment separators.");
                return false;
            }

            stepResult.AddInfo("AvevaTag contains segment separators.");
            return true;
        }

        private bool CheckDuplicate(string? avevaTag, ParserStepResult stepResult)
        {
            if (avevaTag == null)
                return false;

            return false;
        }

        #endregion


        // ─────────────────────────────────────────────────────────────
        // Quality Scoring
        // ─────────────────────────────────────────────────────────────

        #region Quality Scoring

        /// <summary>
        /// Basic surface-level quality logic.
        /// Maps the boolean check results onto the quality score model.
        /// </summary>
        private void ApplySurfaceQuality(ParserResult result, bool exists, bool notEmpty, bool hasSeparator)
        {
            // SyntaxScore — does it resemble a tag?
            result.Quality.SyntaxScore =
                (exists ? 0.3 : 0.0) +
                (notEmpty ? 0.4 : 0.0) +
                (hasSeparator ? 0.3 : 0.0);

            // CompletenessScore — does it contain basic components?
            result.Quality.CompletenessScore =
                (notEmpty && hasSeparator) ? 0.6 : 0.0;

            // Other scores are surface-level so leave them as-is:
            // SemanticScore (left for Validator)
            // NormalizationScore (left for NamingEngine)
        }

        #endregion


        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        #region Helpers

        private XAttribute? GetAttributeNormalized(XElement element, PlantGatewayAttribute attr)
        {
            var target = NormalizeAttributeKey(attr.ToString());

            return element
                .Attributes()
                .ToDictionary(
                    a => NormalizeAttributeKey(a.Name.LocalName),
                    a => a)
                .TryGetValue(target, out var match)
                    ? match
                    : null;
        }


        private string NormalizeAttributeKey(string attributeKey)
        {
            if (string.IsNullOrWhiteSpace(attributeKey))
                return string.Empty;

            // lowercase
            attributeKey = attributeKey.ToLowerInvariant();

            // remove separators
            attributeKey = attributeKey.Replace("_", "").Replace("-", "").Replace(".", "");

            // return clean attribute key
            return attributeKey;
        }

        #endregion
    }
}
