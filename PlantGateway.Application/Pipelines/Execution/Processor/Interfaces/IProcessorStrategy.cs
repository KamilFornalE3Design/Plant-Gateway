using SMSgroup.Aveva.Config.Models.Contracts;

namespace PlantGateway.Application.Pipelines.Execution.Processor.Interfaces
{
    public interface IProcessorStrategy<TDto>
    {
        PipelineContract<TDto> Process(PipelineContract<TDto> pipelineContract);
    }
}
