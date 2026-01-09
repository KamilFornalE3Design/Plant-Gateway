using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Application.Pipelines.Events
{
    public sealed class ExecutionEvent
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ExecutionSeverity Severity { get; set; } = ExecutionSeverity.Info;

        public string Code { get; set; } = string.Empty; // optional event ID
        public string Message { get; set; } = string.Empty;

        public object Details { get; set; } = null;

        public override string ToString()
            => $"[{Severity}] {Message}";
    }

    public enum ExecutionSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
