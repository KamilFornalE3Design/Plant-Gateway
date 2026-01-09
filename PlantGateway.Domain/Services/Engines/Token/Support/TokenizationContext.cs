using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Domain.Services.Engines.Token
{
    /// <summary>
    /// Internal working state for the TokenEngine pipeline.
    /// 
    /// This is <b>not</b> exposed to callers. Engines and stages mutate
    /// this context and, at the end, TokenEngine projects it into a
    /// public <see cref="TokenEngineResult"/>.
    /// </summary>
    public sealed class TokenizationContext
    {
        public bool IsValid { get; set; }
        public bool IsConsistencyChecked { get; set; }
        public int Score0To100 { get; set; }


        /// <summary>
        /// ID of the DTO / source record being tokenized.
        /// Mirrors <see cref="TokenEngineResult.SourceDtoId"/>.
        /// </summary>
        public Guid SourceDtoId { get; set; }

        /// <summary>
        /// Raw value as received from the DTO (e.g. SMSGroup_General_PBS_FULL_CODE).
        /// Mirrors <see cref="TokenEngineResult.RawInputValue"/>.
        /// </summary>
        public string RawInput { get; set; } = string.Empty;

        /// <summary>
        /// Normalized representation used by the engine (after trimming,
        /// separator replacement, etc.).
        /// Mirrors <see cref="TokenEngineResult.NormalizedInputValue"/>.
        /// </summary>
        public string NormalizedInput { get; set; } = string.Empty;

        /// <summary>
        /// Tokenization parts (usually split by '_' or equivalent) after
        /// normalization. These are the building blocks for structural
        /// and suffix recognition.
        /// </summary>
        public string[] Parts { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Primary recognized tokens (Plant, Unit, Section, Equipment,
        /// Component, Discipline, Entity, etc.).
        /// Keys are token names (e.g. "PlantSection"), case-insensitive.
        /// </summary>
        public Dictionary<string, Token> Tokens { get; set; } = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Tokens that were recognized but excluded from final processing
        /// (for example, conflicting tokens, duplicates, or low-confidence
        /// fallbacks that downstream stages should not use).
        /// </summary>
        public Dictionary<string, Token> ExcludedTokens { get; }
            = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Informational messages produced by stages (codification hits,
        /// fallback notices, structural decisions).
        /// Mirrors <see cref="TokenEngineResult.Message"/>.
        /// </summary>
        public List<string> Message { get; } = new List<string>();

        /// <summary>
        /// Warning messages (partial matches, incomplete codification,
        /// low-confidence fallbacks, PlantSection-level exceptions, etc.).
        /// Mirrors <see cref="TokenEngineResult.Warning"/>.
        /// </summary>
        public List<string> Warning { get; } = new List<string>();

        /// <summary>
        /// Error messages (hard failures, unusable input, internal issues).
        /// Mirrors <see cref="TokenEngineResult.Error"/>.
        /// </summary>
        public List<string> Error { get; } = new List<string>();

        /// <summary>
        /// Total confidence score for the tokenization result
        /// (aggregated from per-token scores).
        /// </summary>
        public int TotalScore { get; set; }

        /// <summary>
        /// Per-token confidence scores. Keys match <see cref="Tokens"/> keys
        /// (e.g. "Plant", "PlantUnit", "PlantSection", "Equipment", "Component").
        /// </summary>
        public Dictionary<string, int> TokenScores { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The last stage that successfully executed for this context.
        /// Useful for diagnostics and partial-pipeline runs.
        /// </summary>
        public TokenizationStageId LastExecutedStage { get; set; }
            = TokenizationStageId.None;

        /// <summary>
        /// Set of stages that have already been executed. Allows stages
        /// or diagnostics to ask "has X already run?".
        /// </summary>
        public HashSet<TokenizationStageId> ExecutedStages { get; }
            = new HashSet<TokenizationStageId>();

        /// <summary>
        /// Convenience flag: true when no errors were recorded.
        /// This does <b>not</b> mean the tokenization is high quality,
        /// only that nothing was classified as a hard error.
        /// </summary>
        public bool IsSuccess => Error.Count == 0;

        #region Helpers for messages

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

        #endregion

        #region Stage-tracking helpers

        /// <summary>
        /// Marks a stage as executed and updates <see cref="LastExecutedStage"/>.
        /// Stages should call this at the end of their Execute() method.
        /// </summary>
        public void MarkStageExecuted(TokenizationStageId stageId)
        {
            LastExecutedStage = stageId;
            ExecutedStages.Add(stageId);
        }

        #endregion

        #region Projection to TokenEngineResult

        /// <summary>
        /// Projects the current internal state into a public
        /// <see cref="TokenEngineResult"/> instance.
        /// 
        /// TokenEngine can call this after the final stage (or after
        /// a partial stage for parser-only runs) and, if needed,
        /// enrich the result with additional flags.
        /// </summary>
        public TokenEngineResult ToResult()
        {
            var result = new TokenEngineResult
            {
                SourceDtoId = SourceDtoId,
                RawInputValue = RawInput,
                NormalizedInputValue = NormalizedInput,
                Tokens = new Dictionary<string, Token>(Tokens, StringComparer.OrdinalIgnoreCase),
                ExcludedTokens = new Dictionary<string, Token>(ExcludedTokens, StringComparer.OrdinalIgnoreCase),
                Message = new List<string>(Message),
                Warning = new List<string>(Warning),
                Error = new List<string>(Error),
                IsValid = IsValid,
                IsConsistencyChecked = IsConsistencyChecked
            };
            // TotalScore / TokenScores can be left internal if you prefer,
            // or you can extend TokenEngineResult later to expose them.

            return result;
        }

        #endregion
    }
}
