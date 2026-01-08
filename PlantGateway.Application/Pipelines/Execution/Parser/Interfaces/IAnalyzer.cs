using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;

namespace PlantGateway.Application.Pipelines.Execution.Parser.Interfaces
{
    public interface IAnalyzer<T>
    {
        void Analyze(T input, ParserResult result);
    }
}
