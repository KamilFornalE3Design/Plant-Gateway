using SMSgroup.Aveva.Config.Models.Contracts;

namespace PlantGateway.Application.Pipelines.Validator.Interfaces
{
    /// <summary>
    /// Factory responsible for resolving the correct validator
    /// implementation based on input format (xml, txt, etc.).
    /// </summary>
    public interface IValidatorFactory
    {
        /// <summary>
        /// Returns an appropriate <see cref="IValidator"/> instance
        /// for the provided <see cref="PipelineContract"/>.
        /// </summary>
        IValidatorStrategy Create<TDto>(PipelineContract<TDto> pipelineContract);
    }
}
