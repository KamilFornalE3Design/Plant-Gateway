using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Application.Pipelines.Validator.Interfaces
{
    /// <summary>
    /// Validator that operates on an element-level artifact
    /// (XML element, DB row, JSON node, etc.).
    /// Untyped common base for format-specific element validators.
    /// </summary>
    public interface IElementValidator
    {
        /// <summary>
        /// Returns true if this validator wants to handle the given element object.
        /// </summary>
        bool CanHandle(object element);

        /// <summary>
        /// Performs validation on the given element object.
        /// </summary>
        void Validate(object element, ValidatorResult result);
    }
}
