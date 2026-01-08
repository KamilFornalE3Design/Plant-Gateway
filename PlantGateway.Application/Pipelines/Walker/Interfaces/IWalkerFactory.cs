using SMSgroup.Aveva.Config.Models.Contracts;

namespace PlantGateway.Application.Pipelines.Walker.Interfaces
{
    public interface IWalkerFactory
    {
        IWalkerStrategy<TDto> Create<TDto>(PipelineContract<TDto> pipelineContract);
    }
}
