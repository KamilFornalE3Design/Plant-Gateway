using SMSgroup.Aveva.Config.Models.Contracts;

namespace PlantGateway.Application.Pipelines.Execution.Planner.Interfaces
{
    /// <summary>
    /// Factory responsible for resolving the correct planner
    /// based on the current pipeline context (input, schema, or mode).
    /// </summary>
    public interface IPlannerFactory
    {
        /// <summary>
        /// Creates a planner instance appropriate for the current pipeline.
        /// </summary>
        /// <param name="pipeline">The active pipeline contract containing all contextual data.</param>
        /// <returns>An <see cref="IPlanner"/> implementation suitable for this pipeline.</returns>
        IPlanner<TDto> Create<TDto>(PipelineContract<TDto> pipeline);
    }
}
