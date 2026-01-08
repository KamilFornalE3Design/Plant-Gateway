using SMSgroup.Aveva.Config.Abstractions;
using System;
using System.Collections.Generic;

namespace PlantGateway.Application.Pipelines.Results.Engines
{
    public class RoleEngineResult : IEngineResult
    {
        public Guid SourceDtoId { get; set; }
        public bool IsSuccess =>
            IsValid &&
            !string.IsNullOrEmpty(AvevaType);

        public bool IsLeaf { get; set; }
        public bool IsValid { get; set; }

        public string AvevaType { get; set; } = string.Empty;

        public List<string> Message { get; set; } = new List<string>();
        public List<string> Warning { get; set; } = new List<string>();
        public List<string> Error { get; set; } = new List<string>();

        public void AddMessage(string text) { if (!string.IsNullOrWhiteSpace(text)) Message.Add(text); }
        public void AddWarning(string text) { if (!string.IsNullOrWhiteSpace(text)) Warning.Add(text); }
        public void AddError(string text) { if (!string.IsNullOrWhiteSpace(text)) Error.Add(text); }
    }
}
