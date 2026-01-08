using SMSgroup.Aveva.Config.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Application.Pipelines.Results.Execution
{
    public sealed class ParserStepResult : IStepResult
    {
        // ─────────────────────────────────────────────────────────────
        // Core Properties
        // ─────────────────────────────────────────────────────────────

        #region Core Properties

        public string StepName { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        #endregion

        // ─────────────────────────────────────────────────────────────
        // Diagnostic Events
        // ─────────────────────────────────────────────────────────────

        #region Diagnostic Events

        public List<ExecutionEvent> Events { get; } = new List<ExecutionEvent>();

        public bool IsSuccess =>
            Events.TrueForAll(e => e.Severity != ExecutionSeverity.Error &&
                                   e.Severity != ExecutionSeverity.Critical);

        #endregion

        // ─────────────────────────────────────────────────────────────
        // Helper Methods
        // ─────────────────────────────────────────────────────────────

        #region Helper Methods

        public void AddEvent(ExecutionEvent ev)
        {
            if (ev != null)
                Events.Add(ev);
        }

        public void AddInfo(string message, object details = null)
            => Events.Add(new ExecutionEvent { Severity = ExecutionSeverity.Info, Message = message, Details = details });

        public void AddWarning(string message, object details = null)
            => Events.Add(new ExecutionEvent { Severity = ExecutionSeverity.Warning, Message = message, Details = details });

        public void AddError(string message, object details = null)
            => Events.Add(new ExecutionEvent { Severity = ExecutionSeverity.Error, Message = message, Details = details });

        #endregion

        // ─────────────────────────────────────────────────────────────
        // Output Formatting
        // ─────────────────────────────────────────────────────────────

        #region Output Formatting

        public override string ToString()
        {
            var state = IsSuccess ? "✅ OK" : "❌ Failed";
            return $"{state} – {StepName} | Events={Events.Count} | {Summary}";
        }

        #endregion
    }
}
