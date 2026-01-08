using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;

namespace PlantGateway.Application.Pipelines.Parser.Interfaces
{
    public interface IAnalyzer<T>
    {
        void Analyze(T input, ParserResult result);
    }
}
