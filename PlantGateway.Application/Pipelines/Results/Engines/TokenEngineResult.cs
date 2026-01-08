using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using SMSgroup.Aveva.Config.Models.PublishViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlantGateway.Application.Pipelines.Results.Engines
{
    /// <summary>
    /// Holds normalized results of name tokenization, schema matching,
    /// discipline/entity detection, and hierarchy resolution.
    /// </summary>
    public class TokenEngineResult : IEngineResult
    {
        public Guid SourceDtoId { get; set; }
        public bool IsSuccess =>
            IsValid &&
            IsConsistencyChecked &&
            RawInputValue != string.Empty &&
            NormalizedInputValue != string.Empty &&
            (Tokens.Count != 0);
        /// <summary>
        /// My XML comment
        /// </summary>
        public bool IsValid { get; set; }
        public bool IsConsistencyChecked { get; set; }

        public List<string> Message { get; set; } = new List<string>();
        public List<string> Warning { get; set; } = new List<string>();
        public List<string> Error { get; set; } = new List<string>();

        public void AddMessage(string text) { if (!string.IsNullOrWhiteSpace(text)) Message.Add(text); }
        public void AddWarning(string text) { if (!string.IsNullOrWhiteSpace(text)) Warning.Add(text); }
        public void AddError(string text) { if (!string.IsNullOrWhiteSpace(text)) Error.Add(text); }

        public string RawInputValue { get; set; } = string.Empty;
        public string NormalizedInputValue { get; set; } = string.Empty;
        public Dictionary<string, Token> Tokens { get; set; } = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Token> ExcludedTokens { get; set; } = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);

        // === Computed Token Flags => to remove, logic moved to Token object

        public bool HasStructuralToken => Tokens.ContainsKey("Structural");
        public bool HasCivilToken => Tokens.ContainsKey("Civil");
        public bool HasMechanicalToken => Tokens.ContainsKey("Mechanical");
        public bool HasElectricalToken => Tokens.ContainsKey("Electrical");
        public bool HasPipingToken => Tokens.ContainsKey("Piping");

        public bool HasIncrementalToken => Tokens.ContainsKey("TagIncremental");

        // 
        public bool HasPlantToken => Tokens.ContainsKey("Plant");
        public bool HasPlantUnitToken => Tokens.ContainsKey("PlantUnit");
        public bool HasPlantSectionToken => Tokens.ContainsKey("PlantSection");
        public bool HasEquipmentToken => Tokens.ContainsKey("Equipment");
        public bool HasComponentToken => Tokens.ContainsKey("Component");

        public bool HasDisciplineToken => Tokens.ContainsKey("Discipline");
        public bool HasEntityToken => Tokens.ContainsKey("Entity");

        // Aggregates
        public bool HasAnyStructureToken =>
            HasPlantToken ||
            HasPlantUnitToken ||
            HasPlantSectionToken ||
            HasEquipmentToken ||
            HasComponentToken;

        /// <summary>
        /// Minimal structure for DB Limbo threshold:
        /// Plant + PlantUnit + Discipline OR Entity.
        /// </summary>
        public bool HasMinimalStructure =>
            HasPlantToken &&
            HasPlantUnitToken &&
            (HasDisciplineToken || HasEntityToken);

        /// <summary>
        /// Full structure for Final Import threshold:
        /// Plant + PlantUnit + PlantSection + (Equipment or Component)
        /// and both Discipline + Entity present (directly or by inheritance).
        /// </summary>
        public bool HasFullStructure =>
            HasPlantToken &&
            HasPlantUnitToken &&
            HasPlantSectionToken &&
            (HasEquipmentToken || HasComponentToken) &&
            HasDisciplineToken &&
            HasEntityToken;


        public TokenPhaseView ToPhaseView(ProcessPhase phase)
        {
            // Common pieces
            var view = new TokenPhaseView
            {
                Phase = phase,
                RawInput = RawInputValue,
                NormalizedInput = NormalizedInputValue,
                IsValid = IsValid,
                Score0To100 = ComputeScore0To100(), // you might already have this or add it
                HasMinimalStructure = HasMinimalStructure,
                HasFullStructure = HasFullStructure,
                HasDiscipline = HasDisciplineToken,
                HasEntity = HasEntityToken
            };

            // Phase-specific message projection
            switch (phase)
            {
                case ProcessPhase.Parse:
                    view.SurfaceMessages = BuildParseMessages();
                    view.DetailedMessages = Array.Empty<string>();
                    break;

                case ProcessPhase.Validate:
                    view.SurfaceMessages = BuildParseMessages();
                    view.DetailedMessages = BuildValidateMessages();
                    break;

                case ProcessPhase.Plan:
                    view.SurfaceMessages = BuildPlanMessages();
                    view.DetailedMessages = BuildValidateMessages();
                    break;

                case ProcessPhase.Full:
                default:
                    view.SurfaceMessages = BuildPlanMessages();
                    view.DetailedMessages = BuildValidateMessages();
                    break;
            }

            return view;
        }

        // Below helpers can be private methods on TokenEngineResult.

        private int ComputeScore0To100()
        {
            // For now you can do a simple mapping or expose TotalScore if you have it.
            // Placeholder implementation:
            return IsValid ? 100 : 0;
        }

        private IReadOnlyList<string> BuildParseMessages()
        {
            var list = new List<string>();

            if (string.IsNullOrWhiteSpace(RawInputValue))
                list.Add("AvevaTag is empty or whitespace.");
            else
                list.Add($"AvevaTag present: '{RawInputValue}'.");

            if (HasAnyStructureToken)
                list.Add("Structural pattern detected in tag.");
            else
                list.Add("No recognizable structural segments detected in tag.");

            if (HasDisciplineToken || HasEntityToken)
                list.Add("Discipline/Entity hints present.");
            else
                list.Add("No Discipline/Entity hints detected.");

            return list;
        }

        private IReadOnlyList<string> BuildValidateMessages()
        {
            var list = new List<string>();

            // Forward Warning/Error in a compact way.
            foreach (var w in Warning)
            {
                if (!string.IsNullOrWhiteSpace(w))
                    list.Add("[Warning] " + w);
            }

            foreach (var e in Error)
            {
                if (!string.IsNullOrWhiteSpace(e))
                    list.Add("[Error] " + e);
            }

            return list;
        }

        private IReadOnlyList<string> BuildPlanMessages()
        {
            var list = new List<string>();

            if (HasFullStructure)
                list.Add("Disposition candidate: Final import (full structure detected).");
            else if (HasMinimalStructure)
                list.Add("Disposition candidate: DB Limbo (partial but usable structure).");
            else if (HasAnyStructureToken)
                list.Add("Disposition candidate: MDB Limbo (structure hints but not sufficient).");
            else
                list.Add("Disposition candidate: MDB Limbo (no usable structure detected).");

            return list;
        }

    }

    public sealed class Token
    {
        /// <summary>
        /// Base logical key for the token. E.g., "Plant", "Equipment", "Discipline", etc.
        /// </summary>
        public string Key { get; set; }               // base logical key (Plant, Equipment, etc.)
        /// <summary>
        /// Final value of the token after processing.
        /// </summary>
        public string Value { get; set; }             // actual token text (PCM01, 001, etc.)
        public int Position { get; set; }
        public string Type { get; set; }
        public string Pattern { get; set; }
        public bool IsMatch { get; set; }
        public bool IsMissing { get; set; }
        public bool IsReplacement { get; set; }

        /// <summary>Base token that was replaced (Equipment, PlantSection, ...).</summary>
        public string ReplacesKey { get; set; }

        /// <summary>Token that performed the replacement (TagIncremental, PlantLayout*, ...).</summary>
        public string ReplacedBy { get; set; }

        public bool IsFallback { get; set; }
        public string SourceMapKey { get; set; }
        public string Note { get; set; }

        /// <summary>Valid for downstream processing when matched or replacement.</summary>
        public bool IsProcessable => IsMatch || IsReplacement;

        // 🔧 Updated summary logic
        public string MappingSummary =>
            IsReplacement
                ? ( // replacement
                    string.Equals(Key, ReplacesKey, StringComparison.OrdinalIgnoreCase)
                        // same-level replacement
                        ? $"{ReplacesKey}:{ReplacedBy}:{Value}"
                        // cross-level (e.g., Equipment -> Component) 
                        : $"{ReplacesKey}:{Key}:{ReplacedBy}:{Value}"
                  )
                // no replacement
                : $"{Key}:{Key}:{Value}";


        public override string ToString() => MappingSummary;
    }
}
