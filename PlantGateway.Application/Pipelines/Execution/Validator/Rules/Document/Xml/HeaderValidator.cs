using PlantGateway.Application.Pipelines.Execution.Validator.Interfaces;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;
using SMSgroup.Aveva.Utilities.Validator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Execution.Validator.Rules.Document.Xml
{
    /// <summary>
    /// Performs validation of the XML Header section.
    /// Example responsibilities (later):
    /// - Required metadata
    /// - Source system verification
    /// - Schema version checks.
    /// </summary>
    public sealed class HeaderValidator : IXDocumentValidator
    {
        // ---------------------------
        // TYPED API
        // ---------------------------
        public bool CanHandle(XDocument document)
        {
            if (document?.Root == null)
                return false;

            return document.Root.Element("Header") != null;
        }

        public void Validate(XDocument document, ValidatorResult result)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            // TODO: Add header validation rules here.
        }

        // ---------------------------
        // UNTYPED API
        // ---------------------------
        bool IDocumentValidator.CanHandle(object document)
        {
            return document is XDocument xd && CanHandle(xd);
        }

        void IDocumentValidator.Validate(object document, ValidatorResult result)
        {
            if (document is XDocument xd)
                Validate(xd, result);
        }
    }
}
