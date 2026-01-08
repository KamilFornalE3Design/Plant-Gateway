using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;
using SMSgroup.Aveva.Config.Models.ValueObjects;

namespace PlantGateway.Application.Pipelines.Interfaces
{
    /// <summary>
    /// Generic validator contract for strongly-typed inputs.
    /// Analogous to IAnalyzer&lt;T&gt; on the Parser side.
    /// </summary>
    public interface IValidator<T>
    {
        /// <summary>
        /// Performs validation of the given input and records findings in the result.
        /// </summary>
        void Validate(T input, ValidatorResult result);
    }
}
