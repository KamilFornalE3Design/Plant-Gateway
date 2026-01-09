using PlantGateway.Domain.Services.Engines.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Domain.Services.Engines.Disposition
{
    /// <summary>
    /// High-level classification of a single structure/component for import:
    /// - quality bucket (Final / DB Limbo / MDB Limbo),
    /// - routing hints (server / MDB),
    /// - basic success + diagnostic information.
    /// </summary>
    public sealed class DispositionResult : IEngineResult
    {
        /// <summary>
        /// Id of the source DTO (e.g. ProjectStructureDTO.Id).
        /// </summary>
        public Guid SourceDtoId { get; set; }

        /// <summary>
        /// Overall success flag for this engine.
        /// For now: we consider the result successful when a non-Unknown bucket
        /// is assigned and we have a non-empty raw input value.
        /// </summary>
        public bool IsSuccess =>
            QualityBucket != DispositionQualityBucket.Unknown &&
            !string.IsNullOrWhiteSpace(RawInputValue);

        /// <summary>
        /// Logical validity of the disposition (set by stages).
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// True when all consistency checks in the pipeline have been executed.
        /// </summary>
        public bool IsConsistencyChecked { get; set; }

        public List<string> Message { get; set; } = new List<string>();
        public List<string> Warning { get; set; } = new List<string>();
        public List<string> Error { get; set; } = new List<string>();

        public void AddMessage(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                Message.Add(text);
        }

        public void AddWarning(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                Warning.Add(text);
        }

        public void AddError(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                Error.Add(text);
        }

        /// <summary>
        /// Original AvevaTag (or alternative name) used as input.
        /// </summary>
        public string RawInputValue { get; set; } = string.Empty;

        /// <summary>
        /// Normalized representation of the input (optional, for diagnostics).
        /// </summary>
        public string NormalizedInputValue { get; set; } = string.Empty;

        /// <summary>
        /// Final quality bucket for this element:
        /// FinalImport / DbLimbo / MdbLimbo / Unknown.
        /// </summary>
        public DispositionQualityBucket QualityBucket { get; set; } = DispositionQualityBucket.Unknown;

        /// <summary>
        /// Optional route string that HierarchyEngine can interpret
        /// (e.g. 'ProductionHierarchy', 'DbLimboHierarchy', 'MdbLimboHierarchy').
        /// You can later change this to an enum if needed.
        /// </summary>
        public string Route { get; set; } = string.Empty;

        /// <summary>
        /// Logical key of the target import server, resolved from Entity.
        /// Only logged / used for diagnostics for now.
        /// </summary>
        public string TargetServerKey { get; set; } = string.Empty;

        /// <summary>
        /// Logical key or name of the target Aveva MDB, resolved from Plant.
        /// Only logged / used for diagnostics for now.
        /// </summary>
        public string TargetMdbKey { get; set; } = string.Empty;

        // Convenience flags

        public bool IsFinalImport =>
            QualityBucket == DispositionQualityBucket.FinalImport;

        public bool IsLimbo =>
            QualityBucket == DispositionQualityBucket.DbLimbo ||
            QualityBucket == DispositionQualityBucket.MdbLimbo;

        public bool IsDbLimbo =>
            QualityBucket == DispositionQualityBucket.DbLimbo;

        public bool IsMdbLimbo =>
            QualityBucket == DispositionQualityBucket.MdbLimbo;
    }

    /// <summary>
    /// Classification bucket for disposition quality.
    /// </summary>
    public enum DispositionQualityBucket
    {
        Unknown = 0,
        FinalImport = 1,
        DbLimbo = 2,
        MdbLimbo = 3
    }
}
