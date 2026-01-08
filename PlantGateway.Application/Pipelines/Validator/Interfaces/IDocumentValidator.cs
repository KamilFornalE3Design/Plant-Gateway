using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Application.Pipelines.Validator.Interfaces
{
    /// <summary>
    /// Validator that operates on a document-level artifact
    /// (XML file, DB dataset, JSON document, etc.).
    /// Untyped common base for format-specific document validators.
    /// </summary>
    public interface IDocumentValidator
    {
        /// <summary>
        /// Returns true if this validator wants to handle the given document object.
        /// </summary>
        bool CanHandle(object document);

        /// <summary>
        /// Performs validation on the given document object.
        /// </summary>
        void Validate(object document, ValidatorResult result);
    }
}
