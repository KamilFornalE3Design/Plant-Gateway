using using PlantGateway.Domain.Services.Engines.Abstractions;
using System;
using System.Collections.Generic;

namespace PlantGateway.Domain.Services.Engines.Discipline.Results
{
    public class DisciplineEngineResult : IEngineResult
    {
        public Guid SourceDtoId { get; set; }
        public bool IsSuccess =>
            IsValid &&
            !string.IsNullOrEmpty(Discipline);
        public bool IsValid { get; set; } // true if Entity is on the EntityMap, false in any other case.
        public bool IsInherited { get; set; } // True if entity is inherited from a parent object, false if internaly calculated
        public bool IsForeign { get; set; } // True if Entity value reconed as foreign against any owner
        public bool IsDefault { get; set; } // true if fallback (no parent, no local)

        public string Discipline { get; set; } = string.Empty; // Fallback to empty string
        public Guid InheritedFrom { get; set; } // Provider of Entity overwrite

        public List<string> Message { get; set; } = new List<string>();
        public List<string> Warning { get; set; } = new List<string>();
        public List<string> Error { get; set; } = new List<string>();

        public void AddMessage(string text) { if (!string.IsNullOrWhiteSpace(text)) Message.Add(text); }
        public void AddWarning(string text) { if (!string.IsNullOrWhiteSpace(text)) Warning.Add(text); }
        public void AddError(string text) { if (!string.IsNullOrWhiteSpace(text)) Error.Add(text); }
    }
}
