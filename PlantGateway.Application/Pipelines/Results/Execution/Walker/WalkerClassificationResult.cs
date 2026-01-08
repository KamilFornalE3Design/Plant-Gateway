using SMSgroup.Aveva.Config.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Application.Pipelines.Results.Execution
{
    public sealed class WalkerClassificationResult : IExecutionResult
    {
        public bool Value { get; set; }
        public string Header { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Severity { get; set; }
    }
}
