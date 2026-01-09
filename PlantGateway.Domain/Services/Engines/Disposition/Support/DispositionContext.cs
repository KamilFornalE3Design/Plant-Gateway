using SMSgroup.Aveva.Config.Models.EngineResults;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Domain.Services.Engines.Disposition
{
    /// <summary>
    /// Internal working state for the DispositionEngine pipeline.
    /// Stages read/write this context and finally project it to DispositionResult.
    /// </summary>
    public sealed class DispositionContext
    {
        // === Input / external results ===

        /// <summary>
        /// Id of the source DTO (e.g. ProjectStructureDTO.Id).
        /// </summary>
        public Guid SourceDtoId { get; set; }

        /// <summary>
        /// Raw AvevaTag (or alternative name) used as input for disposition.
        /// </summary>
        public string RawInputValue { get; set; } = string.Empty;

        /// <summary>
        /// Normalized representation of the input (optional, for diagnostics).
        /// </summary>
        public string NormalizedInputValue { get; set; } = string.Empty;

        /// <summary>
        /// Result of TokenEngine for this DTO.
        /// Disposition stages base their decisions on these tokens.
        /// </summary>
        public TokenEngineResult TokenResult { get; set; } = default;

        // Later you can attach Discipline/Entity results here if useful:
        // public DisciplineEngineResult DisciplineResult { get; set; }
        // public EntityEngineResult EntityResult { get; set; }

        // === Pipeline diagnostics ===

        public List<string> Message { get; } = new List<string>();
        public List<string> Warning { get; } = new List<string>();
        public List<string> Error { get; } = new List<string>();

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
        /// Logical validity of the disposition after all checks.
        /// Stages can set this based on their internal consistency rules.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// True when consistency checks have been executed.
        /// </summary>
        public bool IsConsistencyChecked { get; set; }

        // === Quality / bucket / routing result (filled by stages) ===

        /// <summary>
        /// Final quality bucket for the element.
        /// </summary>
        public DispositionQualityBucket QualityBucket { get; set; } = DispositionQualityBucket.Unknown;

        /// <summary>
        /// Logical route that HierarchyEngine can interpret,
        /// e.g. "ProductionHierarchy", "DbLimboHierarchy", "MdbLimboHierarchy".
        /// </summary>
        public string Route { get; set; } = string.Empty;

        /// <summary>
        /// Logical key of the target import server (from Entity).
        /// Logged for now, may drive routing later.
        /// </summary>
        public string TargetServerKey { get; set; } = string.Empty;

        /// <summary>
        /// Logical key/name of the target Aveva MDB (from Plant).
        /// Logged for now, may drive routing later.
        /// </summary>
        public string TargetMdbKey { get; set; } = string.Empty;

        // === Internal flags for stages (you’ll extend this progressively) ===

        /// <summary>
        /// True if the raw tag is null/empty/whitespace.
        /// </summary>
        public bool IsTagEmptyOrWhitespace { get; set; }

        /// <summary>
        /// True if TokenEngine produced at least some usable tokens.
        /// </summary>
        public bool HasAnyTokens { get; set; }

        // Structural flags (to be set by DispositionTokenSnapshotStage):

        public bool HasPlant { get; set; }
        public bool HasPlantUnit { get; set; }
        public bool HasPlantSection { get; set; }
        public bool HasEquipment { get; set; }
        public bool HasComponent { get; set; }

        /// <summary>
        /// True if Component is effectively replacing Equipment
        /// (e.g. via replacement semantics in TokenEngine).
        /// </summary>
        public bool EquipmentReplacedByComponent { get; set; }

        // Functional flags (Discipline / Entity):

        public bool HasEffectiveDiscipline { get; set; }
        public bool HasEffectiveEntity { get; set; }

        /// <summary>
        /// True when the DTO meets criteria for final import (before bucket mapping).
        /// </summary>
        public bool IsFinalImportEligible { get; set; }

        /// <summary>
        /// True when the DTO meets criteria for DB Limbo (before bucket mapping).
        /// </summary>
        public bool IsDbLimboEligible { get; set; }

        // === Projection to public result ===

        /// <summary>
        /// Creates a public DispositionResult snapshot from the current context state.
        /// </summary>
        public DispositionResult ToResult()
        {
            var result = new DispositionResult
            {
                SourceDtoId = SourceDtoId,
                RawInputValue = RawInputValue,
                NormalizedInputValue = NormalizedInputValue,
                QualityBucket = QualityBucket,
                Route = Route,
                TargetServerKey = TargetServerKey,
                TargetMdbKey = TargetMdbKey,
                IsValid = IsValid,
                IsConsistencyChecked = IsConsistencyChecked
            };

            // copy diagnostics
            result.Message.AddRange(Message);
            result.Warning.AddRange(Warning);
            result.Error.AddRange(Error);

            return result;
        }
    }
}
