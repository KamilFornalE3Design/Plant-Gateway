using SMSgroup.Aveva.Config.Abstractions;

namespace PlantGateway.Application.Pipelines.Results.Execution
{
    public sealed class ParserClassificationResult : IExecutionResult
    {
        public bool Value { get; set; }
        public string Header { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Severity { get; set; }
    }
}
