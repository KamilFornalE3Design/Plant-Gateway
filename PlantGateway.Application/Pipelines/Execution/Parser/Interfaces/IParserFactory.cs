using SMSgroup.Aveva.Config.Models.Contracts;

namespace PlantGateway.Application.Pipelines.Execution.Parser.Interfaces
{
    public interface IParserFactory
    {
        IParserStrategy Create<TDto>(PipelineContract<TDto> pipelineContract);
    }
}
