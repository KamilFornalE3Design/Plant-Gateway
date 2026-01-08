using SMSgroup.Aveva.Config.Abstractions;
using System;
using System.Collections.Generic;

namespace PlantGateway.Application.Pipelines.Results.Engines
{
    public sealed class SuffixEngineResult : IEngineResult
    {
        public Guid SourceDtoId { get; set; }
        public bool IsSuccess =>
            IsValid;

        public string Suffix { get; set; } = string.Empty;
        public List<string> TokensUsed { get; set; } = new List<string>();

        public List<string> Message { get; set; } = new List<string>();
        public List<string> Warning { get; set; } = new List<string>();
        public List<string> Error { get; set; } = new List<string>();

        public void AddMessage(string text) { if (!string.IsNullOrWhiteSpace(text)) Message.Add(text); }
        public void AddWarning(string text) { if (!string.IsNullOrWhiteSpace(text)) Warning.Add(text); }
        public void AddError(string text) { if (!string.IsNullOrWhiteSpace(text)) Error.Add(text); }

        public bool IsValid { get; set; }
        public bool HasDiscipline { get; set; }// Discipline used?
        public bool HasEntity { get; set; } // Entity used?
        public bool HasAnyTag { get; set; } //common flag for electrical,mechanical,struct,tagcomposite, incremental 
    }
}
