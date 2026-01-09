using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Domain.Services.Engines.Token
{
    /// <summary>
    /// Identifies the internal stages of the tokenization pipeline.
    /// 
    /// These are <b>engine-internal</b> steps inside TokenEngine and are
    /// not the same as high-level pipeline phases (Parser / Validation / Planner / Execution).
    /// </summary>
    public enum TokenizationStageId
    {
        /// <summary>
        /// No stage has been executed yet.
        /// </summary>
        None = 0,

        /// <summary>
        /// Normalize raw input: trimming, separator unification,
        /// pre-splitting into parts, basic sanity checks.
        /// </summary>
        PreProcessing = 10,

        /// <summary>
        /// Codification-first structural detection:
        /// Plant / PlantUnit / PlantSection / Equipment are resolved
        /// using CodificationMap (Plant breakdown structure).
        /// </summary>
        StructuralCodification = 20,

        /// <summary>
        /// Regex-based base-level fallback:
        /// fills gaps left by codification using TokenRegexMap patterns,
        /// including codification-aware exceptions (e.g. BUILDINGS, WALKWAYS).
        /// </summary>
        RegexBaseFallback = 30,

        /// <summary>
        /// Suffix and tag-level recognition:
        /// Component, Discipline, Entity, TagComposite, TagIncremental, etc.
        /// </summary>
        SuffixRecognition = 40,

        /// <summary>
        /// Cross-check structural tokens against codification:
        /// validates parent/child relations and flags inconsistencies.
        /// </summary>
        CodificationValidation = 50,

        /// <summary>
        /// Computes confidence scores (per-token and total) based on
        /// codification matches, regex fallbacks, and exception handling.
        /// </summary>
        Scoring = 60,

        /// <summary>
        /// Final cleanup and shaping:
        /// separates excluded tokens, applies ordering, and runs
        /// consistency checks before projecting to TokenEngineResult.
        /// </summary>
        PostProcessing = 70
    }
}
