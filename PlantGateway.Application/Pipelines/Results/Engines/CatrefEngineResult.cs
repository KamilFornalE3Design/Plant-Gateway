using SMSgroup.Aveva.Config.Abstractions;
using System;
using System.Collections.Generic;

namespace PlantGateway.Application.Pipelines.Results.Engines
{
    public class CatrefEngineResult : IEngineResult
    {
        public Guid SourceDtoId { get; set; }
        public bool IsValid { get; set; }
        public bool IsSuccess =>
            IsValid &&
            Error.Count == 0;
        // Input Raw Catref
        public string RawCatref { get; set; }
        // Resolved Catref
        public string Catref { get; set; }
        // How was the Catref resolved
        public CatrefResolutionFlags CatrefResolutionFlag { get; set; }
        //public string GeometryType { get; set; }

        public List<string> Message { get; set; } = new List<string>();
        public List<string> Warning { get; set; } = new List<string>();
        public List<string> Error { get; set; } = new List<string>();

        public void AddMessage(string text) { if (!string.IsNullOrWhiteSpace(text)) Message.Add(text); }
        public void AddWarning(string text) { if (!string.IsNullOrWhiteSpace(text)) Warning.Add(text); }
        public void AddError(string text) { if (!string.IsNullOrWhiteSpace(text)) Error.Add(text); }
    }
    [Flags]
    public enum CatrefResolutionFlags
    {
        RawCatref = 1,
        ConstructKey = 2,
        FromDesc = 3,
        UsedBypassCYLI = 4,
        UsedDefault = 5,
    }
}
