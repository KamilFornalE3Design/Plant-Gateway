using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Validator.Interfaces
{
    /// <summary>
    /// XML document validator: operates on XDocument and implements the common IDocumentValidator.
    /// </summary>
    public interface IXDocumentValidator : IDocumentValidator, IValidator<XDocument>
    {
        /// <summary>
        /// Typed check if this validator wants to handle the given XDocument.
        /// </summary>
        bool CanHandle(XDocument xDocument);

        /// <summary>
        /// Typed validation entry point for XDocument.
        /// </summary>
        void Validate(XDocument xDocument, ValidatorResult result);
    }
}
