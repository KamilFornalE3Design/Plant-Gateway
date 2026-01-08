using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Application.Pipelines.Results.Engines
{
    public sealed class PipelineResult
    {
        public string Phase { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime FinishedAt { get; set; }
        public Dictionary<string, object> PhaseData { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public List<PhaseResult> Phases { get; set; } = new List<PhaseResult>();
        public bool Success => ExitCode == 0;
        public int ExitCode { get; set; }

        public List<string> Message { get; set; } = new List<string>();
        public List<string> Warning { get; set; } = new List<string>();
        public List<string> Error { get; set; } = new List<string>();

        public void AddMessage(string text) { if (!string.IsNullOrWhiteSpace(text)) Message.Add(text); }
        public void AddWarning(string text) { if (!string.IsNullOrWhiteSpace(text)) Warning.Add(text); }
        public void AddError(string text) { if (!string.IsNullOrWhiteSpace(text)) Error.Add(text); }

        public void AddPhase(string name, object data)
        {
            Phases.Add(new PhaseResult { Name = name, Timestamp = DateTime.Now, Data = data });
        }
    }

    public sealed class PhaseResult
    {
        public string Name { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public object Data { get; set; }
    }
}
