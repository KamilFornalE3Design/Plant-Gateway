using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Application.Pipelines.Execution.Validator.Interfaces
{
    /// <summary>
    /// Validator that operates on an attribute-level artifact
    /// (XML attribute, DB column value, JSON property, etc.).
    /// Untyped common base for format-specific attribute validators.
    /// </summary>
    public interface IAttributeValidator
    {
        /// <summary>
        /// Returns true if this validator wants to handle the given attribute object.
        /// </summary>
        bool CanHandle(object attribute);

        /// <summary>
        /// Performs validation on the given attribute object.
        /// </summary>
        void Validate(object attribute, ValidatorResult result);
    }
}
