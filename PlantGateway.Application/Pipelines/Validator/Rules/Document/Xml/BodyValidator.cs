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
    /// Performs validation of the XML body:
    /// Assemblies, Parts, and general structure.
    /// Example responsibilities (later):
    /// - Structural consistency
    /// - No orphan parts
    /// - Expected sections exist.
    /// </summary>
    public sealed class BodyValidator : IXDocumentValidator
    {
        // ---------------------------
        // TYPED API
        // ---------------------------
        public bool CanHandle(XDocument document)
        {
            if (document?.Root == null)
                return false;

            // Body applies when the document contains known payload elements
            return document.Root.Descendants("Assembly").Any()
                || document.Root.Descendants("Part").Any();
        }

        public void Validate(XDocument document, ValidatorResult result)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            // TODO: Add body validation logic here.
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
