using SMSgroup.Aveva.Config.Models.Contracts;

namespace PlantGateway.Application.Pipelines.Execution.Walker.Interfaces
{
    public interface IWalkerFactory
    {
        IWalkerStrategy<TDto> Create<TDto>(PipelineContract<TDto> pipelineContract);
    }
}
