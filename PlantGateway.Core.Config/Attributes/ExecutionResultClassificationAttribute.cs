using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Core.Config.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ExecutionResultClassificationAttribute : Attribute
    {
        public string TrueHeader { get; }
        public string FalseHeader { get; }
        public string Description { get; }
        public string Category { get; }
        public string Severity { get; }

        public ExecutionResultClassificationAttribute(
            string trueHeader,
            string falseHeader,
            string description,
            string category = "General",
            string severity = "Info")
        {
            TrueHeader = trueHeader;
            FalseHeader = falseHeader;
            Description = description;
            Category = category;
            Severity = severity;
        }
    }
}
