using PlantGateway.Application.Pipelines.Execution.Validator.Interfaces;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;
using SMSgroup.Aveva.Utilities.Validator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Execution.Validator.Rules.Elements
{
    /// <summary>
    /// Validator for Part elements.
    /// Validation phase only:
    /// - executes a set of Part-related validation rules
    ///   (geom attribute, AvevaTag structure, matrix presence, etc.)
    /// </summary>
    public sealed class PartNodeValidator : IXElementValidator
    {
        private const string PartLocalName = "Part";

        // All XElement rules injected from DI.
        // Individual rules decide applicability via CanEvaluate(xElement).
        private readonly IReadOnlyList<IValidationRule<XElement>> _rules;

        public PartNodeValidator(IEnumerable<IValidationRule<XElement>> rules)
        {
            _rules = (rules ?? Enumerable.Empty<IValidationRule<XElement>>()).ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // Typed IXElementValidator API
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if this validator is interested in the given element.
        /// </summary>
        public bool CanHandle(XElement xElement)
        {
            if (xElement == null)
                return false;

            return xElement.Name.LocalName.Equals(PartLocalName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Executes all applicable validation rules for the given Part element.
        /// </summary>
        public void Validate(XElement input, ValidatorResult result)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            // Generic rule loop – rules decide if they apply to this element.
            foreach (var rule in _rules)
            {
                if (rule.CanEvaluate(input))
                {
                    rule.Evaluate(input, result);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Untyped IElementValidator API (bridge)
        // ─────────────────────────────────────────────────────────────

        bool IElementValidator.CanHandle(object element) =>
            element is XElement el && CanHandle(el);

        void IElementValidator.Validate(object element, ValidatorResult result)
        {
            if (element is XElement el)
            {
                Validate(el, result);
            }
        }
    }
}
