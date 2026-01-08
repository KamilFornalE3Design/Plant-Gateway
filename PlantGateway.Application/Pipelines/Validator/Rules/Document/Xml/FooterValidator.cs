using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;
using SMSgroup.Aveva.Utilities.Validator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Validator.Rules.Document.Xml
{
    /// <summary>
    /// Performs footer / global consistency validation.
    /// Example responsibilities (later):
    /// - Final structure sanity checks
    /// - Cross-checks between header and body
    /// - Global metadata vs actual content validation.
    /// </summary>
    public sealed class FooterValidator : IXDocumentValidator
    {
        // ---------------------------
        // TYPED API
        // ---------------------------
        public bool CanHandle(XDocument document)
        {
            // Footer validator applies to ALL XML documents that were successfully parsed.
            return document?.Root != null;
        }

        public void Validate(XDocument document, ValidatorResult result)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            // TODO: Add footer/global validation logic here.
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
