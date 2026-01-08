using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Walker;
using SMSgroup.Aveva.Config.Models.ValueObjects;

namespace PlantGateway.Application.Pipelines.Walker.Interfaces
{
    /// <summary>
    /// Defines a contract for reading input and producing DTOs.
    /// </summary>
    /// <typeparam name="TDto">The DTO type produced by this walker strategy.</typeparam>
    public interface IWalkerStrategy<TDto>
    {
        /// <summary>
        /// The input format this strategy supports (XML, TXT, etc.).
        /// </summary>
        InputDataFormat Format { get; }

        /// <summary>
        /// Reads the input PipelineContract.Input.File and produces a list of DTOs.
        /// </summary>
        WalkerResult<TDto> Walk(PipelineContract<TDto> pipelineContract);
    }
}
