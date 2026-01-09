using PlantGateway.Domain.Services.Engines.Abstractions;
using PlantGateway.Application.Pipelines.Results.Engines;
using PlantGateway.Domain.Specifications.Maps;

namespace PlantGateway.Domain.Services.Engines.Token
{
    /// <summary>
    /// Stage that validates the structural tokens (Plant / PlantUnit / PlantSection / Equipment / Component)
    /// against the company codification map.
    ///
    /// Design principles:
    /// - Codification is used to *enhance* quality, not as a hard gate.
    /// - Many structures are only partially codified; missing codification is a warning, not a fatal error.
    /// - The structural chain can end either at Equipment or at Component:
    ///     Plant -> PlantUnit -> PlantSection -> Equipment
    ///     Plant -> PlantUnit -> PlantSection -> Component   (discipline-specific variant)
    /// - Validation adjusts per-token scores and adds codification-aware messages / warnings.
    /// - No regex is used here; we only interpret tokens already present in the context.
    /// </summary>
    public sealed class CodificationValidationStage : ITokenizationStage
    {
        public TokenizationStageId Id => TokenizationStageId.CodificationValidation;

        private readonly CodificationMapDTO _codificationMap;

        public CodificationValidationStage(CodificationMapDTO codificationMap)
        {
            _codificationMap = codificationMap ?? throw new ArgumentNullException(nameof(codificationMap));
        }

        public void Execute(TokenizationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var cod = _codificationMap.Codifications;
            if (cod == null || cod.Count == 0)
            {
                context.AddMessage("ℹ️ CodificationValidation: codification map is empty – skipping structural validation.");
                return;
            }

            // Fetch structural tokens (if present)
            context.Tokens.TryGetValue("Plant", out var plantTok);
            context.Tokens.TryGetValue("PlantUnit", out var unitTok);
            context.Tokens.TryGetValue("PlantSection", out var sectionTok);
            context.Tokens.TryGetValue("Equipment", out var equipmentTok);
            context.Tokens.TryGetValue("Component", out var componentTok);

            var hasPlant = IsSolidToken(plantTok);
            var hasUnit = IsSolidToken(unitTok);
            var hasSection = IsSolidToken(sectionTok);
            var hasEquipment = IsSolidToken(equipmentTok);
            var hasComponent = IsSolidToken(componentTok);

            if (!hasPlant && !hasUnit && !hasSection)
            {
                context.AddWarning("⚠️ CodificationValidation: no structural tokens (Plant/Unit/Section) found to validate.");
                return;
            }

            // Extract prefixes (AGL01 -> AGL) as used in codification
            var plantPrefix = hasPlant ? GetPrefix(plantTok.Value) : null;
            var unitPrefix = hasUnit ? GetPrefix(unitTok.Value) : null;
            var sectionPrefix = hasSection ? GetPrefix(sectionTok.Value) : null;
            var equipmentPrefix = hasEquipment ? GetPrefix(equipmentTok.Value) : null;

            // Resolve codification DTOs when possible
            CodificationDTO plantCod = TryGetCodification(cod, plantPrefix, CodificationType.Plant);
            CodificationDTO unitCod = TryGetCodification(cod, unitPrefix, CodificationType.PlantUnit);
            CodificationDTO sectionCod = TryGetCodification(cod, sectionPrefix, CodificationType.PlantSection);
            CodificationDTO equipmentCod = TryGetCodification(cod, equipmentPrefix, CodificationType.Equipment);

            // === 1️⃣ Validate Plant ===
            if (hasPlant)
            {
                if (plantCod != null)
                {
                    AddScore(context, "Plant", +8);
                    context.AddMessage($"🏷 CodificationValidation: Plant '{plantTok.Value}' recognized as code '{plantCod.Code}'.");
                }
                else
                {
                    AddScore(context, "Plant", -4);
                    context.AddWarning(
                        $"⚠️ CodificationValidation: Plant '{plantTok.Value}' (code '{plantPrefix}') is not present in codification map. Treating as uncodified plant.");
                }
            }

            // === 2️⃣ Validate PlantUnit under Plant ===
            if (hasUnit)
            {
                if (unitCod != null)
                {
                    // When plantCod known, verify parent
                    if (plantCod != null)
                    {
                        if (string.Equals(unitCod.ParentCode, plantCod.Code, StringComparison.OrdinalIgnoreCase))
                        {
                            AddScore(context, "PlantUnit", +8);
                            context.AddMessage(
                                $"🏷 CodificationValidation: PlantUnit '{unitTok.Value}' (code '{unitCod.Code}') is correctly registered under Plant '{plantCod.Code}'.");
                        }
                        else
                        {
                            AddScore(context, "PlantUnit", -10);
                            context.AddWarning(
                                $"⚠️ CodificationValidation: PlantUnit '{unitTok.Value}' (code '{unitCod.Code}') is not registered under Plant '{plantCod.Code}' (parent in codification: '{unitCod.ParentCode ?? "<none>"}').");
                        }
                    }
                    else
                    {
                        // Plant unknown, but Unit known – still useful
                        AddScore(context, "PlantUnit", +3);
                        context.AddMessage(
                            $"ℹ️ CodificationValidation: PlantUnit '{unitTok.Value}' (code '{unitCod.Code}') is known, but Plant is uncodified.");
                    }
                }
                else
                {
                    AddScore(context, "PlantUnit", -4);
                    context.AddWarning(
                        $"⚠️ CodificationValidation: PlantUnit '{unitTok.Value}' (code '{unitPrefix}') is not present in codification map.");
                }
            }

            // === 3️⃣ Validate PlantSection under PlantUnit ===
            if (hasSection)
            {
                if (sectionCod != null)
                {
                    if (unitCod != null)
                    {
                        if (string.Equals(sectionCod.ParentCode, unitCod.Code, StringComparison.OrdinalIgnoreCase))
                        {
                            AddScore(context, "PlantSection", +8);
                            context.AddMessage(
                                $"🏷 CodificationValidation: PlantSection '{sectionTok.Value}' (code '{sectionCod.Code}') is correctly registered under PlantUnit '{unitCod.Code}'.");
                        }
                        else
                        {
                            AddScore(context, "PlantSection", -10);
                            context.AddWarning(
                                $"⚠️ CodificationValidation: PlantSection '{sectionTok.Value}' (code '{sectionCod.Code}') is not registered under PlantUnit '{unitCod.Code}' (parent in codification: '{sectionCod.ParentCode ?? "<none>"}').");
                        }
                    }
                    else
                    {
                        AddScore(context, "PlantSection", +3);
                        context.AddMessage(
                            $"ℹ️ CodificationValidation: PlantSection '{sectionTok.Value}' (code '{sectionCod.Code}') is known, but PlantUnit is uncodified.");
                    }
                }
                else
                {
                    AddScore(context, "PlantSection", -4);
                    context.AddWarning(
                        $"⚠️ CodificationValidation: PlantSection '{sectionTok.Value}' (code '{sectionPrefix}') is not present in codification map. PlantSection-level exceptions (PlantLayout*/BUILDINGS/WALKWAYS/...) are still allowed via regex fallback.");
                }
            }

            // === 4️⃣ Validate Equipment under PlantSection ===
            // Note: structures are allowed to end at Equipment OR at Component.
            if (hasEquipment)
            {
                if (equipmentCod != null)
                {
                    if (sectionCod != null)
                    {
                        if (string.Equals(equipmentCod.ParentCode, sectionCod.Code, StringComparison.OrdinalIgnoreCase))
                        {
                            AddScore(context, "Equipment", +8);
                            context.AddMessage(
                                $"🏷 CodificationValidation: Equipment '{equipmentTok.Value}' (code '{equipmentCod.Code}') is correctly registered under PlantSection '{sectionCod.Code}'.");
                        }
                        else
                        {
                            AddScore(context, "Equipment", -10);
                            context.AddWarning(
                                $"⚠️ CodificationValidation: Equipment '{equipmentTok.Value}' (code '{equipmentCod.Code}') is not registered under PlantSection '{sectionCod.Code}' (parent in codification: '{equipmentCod.ParentCode ?? "<none>"}').");
                        }
                    }
                    else
                    {
                        AddScore(context, "Equipment", +3);
                        context.AddMessage(
                            $"ℹ️ CodificationValidation: Equipment '{equipmentTok.Value}' (code '{equipmentCod.Code}') is known, but PlantSection is uncodified.");
                    }
                }
                else
                {
                    // Equipment value not codified – allowed (map may be incomplete), but slightly lower confidence
                    AddScore(context, "Equipment", -3);
                    context.AddWarning(
                        $"⚠️ CodificationValidation: Equipment '{equipmentTok.Value}' (code '{equipmentPrefix}') is not present in codification map. Treating as uncodified equipment.");
                }
            }
            else if (hasComponent)
            {
                // Allowed discipline-specific pattern: Plant -> Unit -> Section -> Component (no Equipment)
                AddScore(context, "Component", +2);
                context.AddMessage(
                    $"ℹ️ CodificationValidation: no Equipment token, but Component '{componentTok.Value}' is present. Treating Plant-Unit-Section-Component as valid discipline-specific structural tip.");
            }

            // === 5️⃣ High-level summary (if at least Plant/Unit/Section chain is coherent) ===
            if (plantCod != null && unitCod != null && sectionCod != null &&
                string.Equals(unitCod.ParentCode, plantCod.Code, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(sectionCod.ParentCode, unitCod.Code, StringComparison.OrdinalIgnoreCase))
            {
                var tip = hasEquipment
                    ? $"Equipment '{equipmentTok?.Value}'"
                    : hasComponent
                        ? $"Component '{componentTok?.Value}'"
                        : "no Equipment/Component";

                context.AddMessage(
                    $"✅ CodificationValidation: structural chain Plant '{plantTok?.Value}' → Unit '{unitTok?.Value}' → Section '{sectionTok?.Value}' → {tip} is codification-consistent (where codification data is available).");
            }
        }

        // =====================================================================
        //  Helpers
        // =====================================================================

        private static bool IsSolidToken(Token token)
        {
            if (token == null)
                return false;

            if (token.IsMissing)
                return false;

            if (string.IsNullOrWhiteSpace(token.Value))
                return false;

            if (token.Value.StartsWith("MISSING_", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        /// <summary>
        /// Get the three-letter prefix used as codification code
        /// (e.g. AGL01 -> AGL).
        /// </summary>
        private static string GetPrefix(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
                return string.Empty;

            return segment.Length >= 3
                ? segment.Substring(0, 3)
                : segment;
        }

        private static CodificationDTO TryGetCodification(
            IReadOnlyDictionary<string, CodificationDTO> codifications,
            string prefix,
            CodificationType expectedType)
        {
            if (codifications == null ||
                string.IsNullOrWhiteSpace(prefix) ||
                !codifications.TryGetValue(prefix, out var dto) ||
                dto == null)
            {
                return null;
            }

            // If type mismatches, we treat it as "no codification for this level"
            if (dto.CodificationType != CodificationType.Undefined &&
                dto.CodificationType != expectedType)
            {
                return null;
            }

            return dto;
        }

        private static void AddScore(TokenizationContext context, string tokenKey, int delta)
        {
            if (context == null || string.IsNullOrWhiteSpace(tokenKey))
                return;

            if (!context.TokenScores.TryGetValue(tokenKey, out var current))
                current = 0;

            current += delta;
            context.TokenScores[tokenKey] = current;
            context.TotalScore += delta;
        }
    }
}
