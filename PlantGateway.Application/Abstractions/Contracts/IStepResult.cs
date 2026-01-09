using PlantGateway.Application.Pipelines.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Application.Abstractions.Contracts
{
    /// <summary>
    /// Represents a common interface for ParserStepResult and ValidatorStepResult.
    /// Each step produces messages (events) + structured metadata.
    /// </summary>
    public interface IStepResult
    {
        string StepName { get; set; }
        string Summary { get; set; }

        /// <summary>
        /// True if no fatal errors occurred in this step.
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// Unified list of severity-based events.
        /// </summary>
        List<ExecutionEvent> Events { get; }

        /// <summary>
        /// Structured metadata produced by the step.
        /// JSON-friendly, DB-friendly.
        /// </summary>
        Dictionary<string, object> Data { get; }

        void AddEvent(ExecutionEvent ev);
        void AddInfo(string message, object details = null);
        void AddWarning(string message, object details = null);
        void AddError(string message, object details = null);
    }
}
