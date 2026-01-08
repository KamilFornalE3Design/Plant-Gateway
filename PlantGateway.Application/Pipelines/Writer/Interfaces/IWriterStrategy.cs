using SMSgroup.Aveva.Config.Models.Contracts;

namespace PlantGateway.Application.Pipelines.Writer.Interfaces
{
    /// <summary>
    /// Defines a strategy for writing enriched DTOs to an output target.
    /// </summary>
    public interface IWriterStrategy<TDto>
    {
        /// <summary>
        /// Writes DTOs to the specified output target.
        /// </summary>
        void Write(PipelineContract<TDto> pipelineContract);
    }
}
