using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.Tokenization;
using SMSgroup.Aveva.Utilities.Helpers;

namespace PlantGateway.Domain.Services.Engines.Token
{
    /// <summary>
    /// Stage that tries to recognize Plant / PlantUnit / PlantSection / Equipment
    /// using the company codification map (CodificationMapDTO).
    /// 
    /// Design:
    /// - Codification-first, regex-second.
    /// - Does NOT mark tokens as missing. If codification is incomplete,
    ///   later regex stages can still fill gaps.
    /// - Adds codification-aware info / warning messages and bumps token scores.
    /// </summary>
    public sealed class StructuralCodificationStage : ITokenizationStage
    {
        public TokenizationStageId Id => TokenizationStageId.StructuralCodification;

        private readonly CodificationMapDTO _codificationMap;

        public StructuralCodificationStage(CodificationMapDTO codificationMap)
        {
            _codificationMap = codificationMap ?? throw new ArgumentNullException(nameof(codificationMap));
        }

        public void Execute(TokenizationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var cod = _codificationMap.Codifications;
            if (cod == null || cod.Count == 0)
            {
                context.AddMessage("ℹ️ StructuralCodification: codification map is empty – skipping structural detection.");
                return;
            }

            // === 1️⃣ Ensure we have Parts (segments) ===
            var parts = context.Parts;
            if (parts == null || parts.Length == 0)
            {
                // Prefer already-normalized input; otherwise fall back to raw
                var source = !string.IsNullOrWhiteSpace(context.NormalizedInput)
                    ? context.NormalizedInput
                    : context.RawInput?.Trim().ToUpperInvariant() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(source))
                {
                    context.AddWarning("⚠️ StructuralCodification: no input available for codification.");
                    return;
                }

                // Normalize separators and split into parts
                var normalized = SeparatorHelper.Normalize(source);
                parts = SeparatorHelper.SplitNormalized(normalized);

                context.NormalizedInput = normalized;
                context.Parts = parts;
            }

            if (parts.Length == 0)
            {
                context.AddWarning("⚠️ StructuralCodification: input normalization produced no segments.");
                return;
            }

            // === 2️⃣ Try to recognize structural levels from codification ===
            // We work left-to-right and, for each segment, look at its 3-letter prefix.
            Token? plantToken = null;
            Token? unitToken = null;
            Token? sectionToken = null;
            Token? equipmentToken = null;

            for (int index = 0; index < parts.Length; index++)
            {
                var segment = parts[index];
                if (string.IsNullOrWhiteSpace(segment))
                    continue;

                var prefix = segment.Length >= 3
                    ? segment.Substring(0, 3)
                    : segment;

                if (!cod.TryGetValue(prefix, out var dto) || dto == null)
                    continue; // not a codified segment

                switch (dto.CodificationType)
                {
                    case CodificationType.Plant:
                        if (plantToken == null && !context.Tokens.ContainsKey("Plant"))
                        {
                            plantToken = new Token
                            {
                                Key = "Plant",
                                Value = segment,
                                Position = index,
                                Type = "Codification",
                                Pattern = "Codification.Plant",
                                IsMatch = true,
                                IsMissing = false,
                                IsReplacement = false,
                                SourceMapKey = prefix,
                                Note = "Plant resolved from codification map."
                            };
                            context.Tokens["Plant"] = plantToken;
                            context.AddMessage($"🏷 StructuralCodification: Plant '{segment}' recognized from code '{prefix}'.");
                        }
                        break;

                    case CodificationType.PlantUnit:
                        if (unitToken == null && !context.Tokens.ContainsKey("PlantUnit"))
                        {
                            unitToken = new Token
                            {
                                Key = "PlantUnit",
                                Value = segment,
                                Position = index,
                                Type = "Codification",
                                Pattern = "Codification.PlantUnit",
                                IsMatch = true,
                                IsMissing = false,
                                IsReplacement = false,
                                SourceMapKey = prefix,
                                Note = "PlantUnit resolved from codification map."
                            };
                            context.Tokens["PlantUnit"] = unitToken;
                            context.AddMessage($"🏷 StructuralCodification: PlantUnit '{segment}' recognized from code '{prefix}'.");
                        }
                        break;

                    case CodificationType.PlantSection:
                        if (sectionToken == null && !context.Tokens.ContainsKey("PlantSection"))
                        {
                            sectionToken = new Token
                            {
                                Key = "PlantSection",
                                Value = segment,
                                Position = index,
                                Type = "Codification",
                                Pattern = "Codification.PlantSection",
                                IsMatch = true,
                                IsMissing = false,
                                IsReplacement = false,
                                SourceMapKey = prefix,
                                Note = "PlantSection resolved from codification map."
                            };
                            context.Tokens["PlantSection"] = sectionToken;
                            context.AddMessage($"🏷 StructuralCodification: PlantSection '{segment}' recognized from code '{prefix}'.");
                        }
                        break;

                    case CodificationType.Equipment:
                        if (equipmentToken == null && !context.Tokens.ContainsKey("Equipment"))
                        {
                            equipmentToken = new Token
                            {
                                Key = "Equipment",
                                Value = segment,
                                Position = index,
                                Type = "Codification",
                                Pattern = "Codification.Equipment",
                                IsMatch = true,
                                IsMissing = false,
                                IsReplacement = false,
                                SourceMapKey = prefix,
                                Note = "Equipment resolved from codification map."
                            };
                            context.Tokens["Equipment"] = equipmentToken;
                            context.AddMessage($"🏷 StructuralCodification: Equipment '{segment}' recognized from code '{prefix}'.");
                        }
                        break;
                }
            }

            // === 3️⃣ Summarize & handle Plant–Unit–Section–Component variant ===
            var hasPlant = plantToken != null || context.Tokens.ContainsKey("Plant");
            var hasUnit = unitToken != null || context.Tokens.ContainsKey("PlantUnit");
            var hasSection = sectionToken != null || context.Tokens.ContainsKey("PlantSection");
            var hasEquipment = equipmentToken != null || context.Tokens.ContainsKey("Equipment");

            if (!hasPlant && !hasUnit && !hasSection && !hasEquipment)
            {
                context.AddMessage("ℹ️ StructuralCodification: no segments resolved from codification – regex fallback will handle structural detection.");
                return;
            }

            // We do *not* treat missing Equipment as an error, because some disciplines
            // use Plant–Unit–Section–Component instead of Plant–Unit–Section–Equipment.
            if (hasPlant && hasUnit && hasSection && !hasEquipment)
            {
                var secPos = sectionToken?.Position ?? context.Tokens["PlantSection"].Position;
                bool tipCandidateExists = secPos >= 0 && parts.Length > secPos + 1;

                if (tipCandidateExists)
                {
                    context.AddMessage(
                        "ℹ️ StructuralCodification: Plant/Unit/Section resolved from codification, " +
                        "but no codified Equipment segment found. This is expected for some disciplines " +
                        "where the chain ends at Component; later stages will treat the last segment as a component candidate.");
                }
                else
                {
                    context.AddMessage(
                        "ℹ️ StructuralCodification: Plant/Unit/Section resolved from codification; " +
                        "no explicit Equipment or Component segment detected.");
                }
            }
            else if (hasPlant && hasUnit && hasSection && hasEquipment)
            {
                context.AddMessage(
                    "✅ StructuralCodification: Plant → Unit → Section → Equipment chain recognized from codification. " +
                    "CodificationValidationStage will verify parent-child relationships.");
            }
            else
            {
                context.AddMessage(
                    "ℹ️ StructuralCodification: partial codification found (one or more of Plant / Unit / Section / Equipment). " +
                    "Remaining structural details will be inferred by regex fallback and validation stages.");
            }
        }

        /// <summary>
        /// Writes a structural base token into the context and bumps the score
        /// for that token key in the TokenizationContext.
        /// </summary>
        private static void AddStructuralToken(
            TokenizationContext context,
            string key,
            string value,
            int position,
            string note)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                return;

            var tokens = context.Tokens;

            var token = new Token
            {
                Key = key,
                Value = value,
                Position = position,
                Type = "Codification",              // structural, from codification map
                Pattern = $"Codification.{key}",       // e.g. Codification.Plant
                IsMatch = true,
                IsMissing = false,
                IsReplacement = false,
                IsFallback = false,
                SourceMapKey = "CodificationMap",
                Note = note
            };

            tokens[key] = token;

            // Optional scoring – codification hit is high confidence
            if (context.TokenScores != null)
            {
                if (!context.TokenScores.TryGetValue(key, out var current))
                    current = 0;

                // Weight customizable later; for now codification hit = +2
                current += 2;
                context.TokenScores[key] = current;

                // Keep TotalScore in sync so ScoringStage can use it
                context.TotalScore += 2;
            }
        }
    }
}
