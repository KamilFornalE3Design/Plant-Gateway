using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using SMSgroup.Aveva.Utilities.Parser.Interfaces;

namespace PlantGateway.Application.Pipelines.Parser.Analyzers.Document.Common
{
    public class FileAnalyzer : IAnalyzer<InputTarget>
    {
        public void Analyze(InputTarget target, ParserResult result)
        {
            if (!File.Exists(target.FilePath))
            {
                result.Warnings.Add($"File not found: {target.FilePath}");
            }
        }
    }
}
