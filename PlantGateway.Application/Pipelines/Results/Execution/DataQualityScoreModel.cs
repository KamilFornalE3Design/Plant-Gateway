using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlantGateway.Application.Pipelines.Results.Execution
{
    /// <summary>
    /// Represents high-level data quality indicators for a parsed or validated input.
    /// 
    /// This container does not compute its own values – it is filled by
    /// pipeline steps or by a later "QualityEngine".
    /// </summary>
    public sealed class DataQualityScoreModel
    {
        // ─────────────────────────────────────────────────────────────
        // Overall Score
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Combined quality score from 0.0 to 1.0. Calculated internaly.
        /// Example: 0.82 means 82% acceptable quality.
        /// </summary>
        public double TotalScore
        {
            get
            {
                var values = new List<double>();

                if (SyntaxScore >= 0) values.Add(SyntaxScore);
                if (SemanticScore >= 0) values.Add(SemanticScore);
                if (CompletenessScore >= 0) values.Add(CompletenessScore);
                if (NormalizationScore >= 0) values.Add(NormalizationScore);

                return values.Count == 0 ? 0.0 : values.Average();
            }
        }


        // ─────────────────────────────────────────────────────────────
        // Quality Dimensions
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Syntax correctness score: tag format, headers, XML structure.
        /// </summary>
        public double SyntaxScore { get; set; }

        /// <summary>
        /// Semantic correctness score: codification rules, hierarchy validity.
        /// </summary>
        public double SemanticScore { get; set; }

        /// <summary>
        /// Completeness score: missing fields, missing tokens, missing attributes.
        /// </summary>
        public double CompletenessScore { get; set; }

        /// <summary>
        /// Normalization score: naming repair success, suffix logic, deterministic identity.
        /// </summary>
        public double NormalizationScore { get; set; }

        // ─────────────────────────────────────────────────────────────
        // Additional Metadata (Optional)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Arbitrary metadata scores or diagnostic values.
        /// JSON-friendly object to allow extensions without schema changes.
        /// Example:
        ///   { "MissingHeaderRatio" : 0.14 }
        ///   { "CodificationDeviationCount" : 3 }
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }
}
