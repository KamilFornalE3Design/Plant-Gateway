using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using System;
using System.Collections.Generic;

namespace PlantGateway.Application.Pipelines.Results.Engines
{
    /// <summary>
    /// Result of the <see cref="HierarchyEngine"/>.
    /// Describes the inferred structural position of a ProjectStructureDTO
    /// within the overall plant hierarchy.
    /// </summary>
    public sealed class HierarchyEngineResult : IEngineResult
    {
        public Guid SourceDtoId { get; set; }
        public bool IsSuccess => 
            IsValid && 
            IsConsistent && 
            Error.Count == 0;
        public bool IsValid { get; set; }
        public string AvevaTag { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<HierarchyNode> HierarchyChain { get; set; } = new List<HierarchyNode>();
        public bool IsConsistent { get; set; }

        public List<string> Message { get; set; } = new List<string>();
        public List<string> Warning { get; set; } = new List<string>();
        public List<string> Error { get; set; } = new List<string>();

        public void AddMessage(string text) { if (!string.IsNullOrWhiteSpace(text)) Message.Add(text); }
        public void AddWarning(string text) { if (!string.IsNullOrWhiteSpace(text)) Warning.Add(text); }
        public void AddError(string text) { if (!string.IsNullOrWhiteSpace(text)) Error.Add(text); }
    }
    public sealed class HierarchyNode
    {
        public Guid Id { get; set; } // must be the same as DTO and input node

        public string AvevaType { get; set; } = string.Empty;
        public string AvevaTag { get; set; } = string.Empty;
        public string ParentAvevaTag { get; set; }
        public Guid? ParentId { get; set; }
        public int Depth { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsConsistent { get; set; }
        public List<HierarchyNode> Children { get; set; } = new List<HierarchyNode>();

        public override string ToString()
        {
            var virt = IsVirtual ? " (Virtual)" : string.Empty;
            return $"{AvevaType}: {AvevaTag}{virt}";
        }
    }
}
