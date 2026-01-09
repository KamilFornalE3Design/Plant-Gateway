using PlantGateway.Domain.Services.Engines.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Domain.Services.Engines.Identity.Results
{
    public class IdentityEngineResult : IEngineResult
    {
        public Guid SourceDtoId { get; set; }
        public Guid Id { get; set; }

        public bool IsValid { get; set; }
        public bool IsSuccess => 
            IsValid && 
            Error.Count == 0;

        public List<string> Message { get; set; } = new List<string>();
        public List<string> Warning { get; set; } = new List<string>();
        public List<string> Error { get; set; } = new List<string>();

        public void AddMessage(string text) { if (!string.IsNullOrWhiteSpace(text)) Message.Add(text); }
        public void AddWarning(string text) { if (!string.IsNullOrWhiteSpace(text)) Warning.Add(text); }
        public void AddError(string text) { if (!string.IsNullOrWhiteSpace(text)) Error.Add(text); }
    }
}
