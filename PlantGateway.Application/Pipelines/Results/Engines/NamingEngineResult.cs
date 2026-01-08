using SMSgroup.Aveva.Config.Abstractions;
using System;
using System.Collections.Generic;

namespace PlantGateway.Application.Pipelines.Results.Engines
{
    public sealed class NamingEngineResult : IEngineResult
    {
        public Guid SourceDtoId { get; set; }
        public bool IsSuccess => 
            IsValid;

        public bool IsValid { get; set; }
        public string BaseName { get; set; } = string.Empty;
        public string NormalizedBaseName { get; set; } = string.Empty;
        public List<string> TokensUsed { get; set; } = new List<string>();

        public List<string> Message { get; set; } = new List<string>();
        public List<string> Warning { get; set; } = new List<string>();
        public List<string> Error { get; set; } = new List<string>();

        public void AddMessage(string text) { if (!string.IsNullOrWhiteSpace(text)) Message.Add(text); }
        public void AddWarning(string text) { if (!string.IsNullOrWhiteSpace(text)) Warning.Add(text); }
        public void AddError(string text) { if (!string.IsNullOrWhiteSpace(text)) Error.Add(text); }
    }
}
