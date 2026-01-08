using SMSgroup.Aveva.Config.Models.Contracts;

namespace PlantGateway.Application.Pipelines.Processor.Interfaces
{
    public interface IProcessorStrategy<TDto>
    {
        PipelineContract<TDto> Process(PipelineContract<TDto> pipelineContract);
    }
}
