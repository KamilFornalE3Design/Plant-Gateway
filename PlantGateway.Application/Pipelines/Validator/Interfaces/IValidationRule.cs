using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;

namespace PlantGateway.Application.Pipelines.Validator.Interfaces
{
    /// <summary>
    /// A generic validation rule that can be executed on a specific type.
    /// </summary>
    public interface IValidationRule<T>
    {
        /// <summary>
        /// Returns true if this rule applies to the given input instance.
        /// </summary>
        bool CanEvaluate(T input);

        /// <summary>
        /// Executes the rule logic and writes results into ValidatorResult.
        /// </summary>
        void Evaluate(T input, ValidatorResult result);
    }
}
