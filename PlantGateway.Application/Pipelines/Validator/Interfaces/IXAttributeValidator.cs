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
    /// XML attribute validator: operates on XAttribute and implements the common IAttributeValidator.
    /// </summary>
    public interface IXAttributeValidator : IAttributeValidator, IValidator<XAttribute>
    {
        /// <summary>
        /// Returns true if this validator wants to handle the given attribute.
        /// </summary>
        bool CanHandle(XAttribute xAttribute);

        /// <summary>
        /// Typed validation entry point for XAttribute.
        /// </summary>
        void Validate(XAttribute xAttribute, ValidatorResult result);
    }
}
