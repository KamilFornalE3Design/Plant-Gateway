using SMSgroup.Aveva.Config.Models.Contracts;

namespace PlantGateway.Application.Pipelines.Writer.Interfaces
{
    /// <summary>
    /// Factory for creating writer strategies for a given DTO type.
    /// Writers serialize processed DTOs to a destination defined in <see cref="OutputTarget"/>.
    /// </summary>
    public interface IWriterFactory
    {
        /// <summary>
        /// Creates a writer strategy for the given DTO type and targets.
        /// </summary>
        /// <typeparam name="TDto">The DTO type to write.</typeparam>
        /// <param name="input">The input contract (source context).</param>
        /// <param name="output">The output contract (destination context).</param>
        /// <returns>A writer strategy for the given type and format.</returns>
        IWriterStrategy<TDto> Create<TDto>(PipelineContract<TDto> pipelineContract);
    }
}
