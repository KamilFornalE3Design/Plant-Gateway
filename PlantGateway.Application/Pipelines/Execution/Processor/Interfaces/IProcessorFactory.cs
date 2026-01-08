using SMSgroup.Aveva.Config.Models.Contracts;

namespace PlantGateway.Application.Pipelines.Execution.Processor.Interfaces
{
    public interface IProcessorFactory
    {
        IProcessorStrategy<TDto> Create<TDto>(PipelineContract<TDto> pipelineContract);
    }
}
