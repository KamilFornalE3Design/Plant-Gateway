using PlantGateway.Domain.Services.Engines.Abstractions;
using System;
using System.Collections.Generic;

namespace PlantGateway.Domain.Services.Engines.Tag.Results
{
    public sealed class TagEngineResult : IEngineResult
    {
        public Guid SourceDtoId { get; set; }
        public bool IsSuccess =>
            IsValid;
        public bool IsValid { get; set; }

        public string Role { get; set; } = string.Empty;
        public string BaseName { get; set; } = string.Empty;
        public string TagSuffix { get; set; } = string.Empty;
        public string FullTag { get; set; } = string.Empty;
        public string NormalizedTag =>
            (FullTag ?? string.Empty)
                .Replace(" ", "_")
                .Replace(".", "_")
                .Replace("-", "_")
                .ToUpperInvariant();

        public List<string> Message { get; set; } = new List<string>();
        public List<string> Warning { get; set; } = new List<string>();
        public List<string> Error { get; set; } = new List<string>();

        public void AddMessage(string text) { if (!string.IsNullOrWhiteSpace(text)) Message.Add(text); }
        public void AddWarning(string text) { if (!string.IsNullOrWhiteSpace(text)) Warning.Add(text); }
        public void AddError(string text) { if (!string.IsNullOrWhiteSpace(text)) Error.Add(text); }
    }
}
