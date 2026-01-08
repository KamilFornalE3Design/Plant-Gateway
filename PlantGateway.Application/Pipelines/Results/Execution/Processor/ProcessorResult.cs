using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Application.Pipelines.Results.Execution
{
    public sealed class ProcessorResult<TDto>
    {
        public List<string> Message { get; set; } = new List<string>();
        public List<string> Warning { get; set; } = new List<string>();
        public List<string> Error { get; set; } = new List<string>();

        public void AddMessage(string message) => Message.Add(message);
        public void AddWarning(string warning) => Warning.Add(warning);
        public void AddError(string error) => Error.Add(error);
    }
}
