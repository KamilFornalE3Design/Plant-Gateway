using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Execution.Validator.Interfaces
{
    /// <summary>
    /// XML element validator: operates on XElement and implements the common IElementValidator.
    /// </summary>
    public interface IXElementValidator : IElementValidator, IValidator<XElement>
    {
        /// <summary>
        /// Returns true if this validator wants to handle the given element.
        /// </summary>
        bool CanHandle(XElement xElement);

        /// <summary>
        /// Typed validation entry point for XElement.
        /// </summary>
        void Validate(XElement xElement, ValidatorResult result);
    }
}
