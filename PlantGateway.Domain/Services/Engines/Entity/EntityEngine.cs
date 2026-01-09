using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.Extensions;

namespace SMSgroup.Aveva.Utilities.Engines
{
    /// <summary>
    /// Resolves the intended Entity (company/department ownership) for a single DTO.
    /// Validation and inheritance control are handled by the caller (strategy).
    /// </summary>
    public sealed class EntityEngine : IEngine
    {
        private readonly IEntityMapService _mapService;

        public EntityEngine(IEntityMapService mapService)
        {
            _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
        }

        public EntityEngineResult Process(TakeOverPointDTO dto)
        {
            var result = new EntityEngineResult
            {

            };

            return result;
        }

        /// <summary>
        /// Processes a single DTO using its TokenEngineResult and optional inherited entity.
        /// </summary>
        public EntityEngineResult Process(ProjectStructureDTO dto, (Guid Id, string Code)? inherited)
        {
            // === Fail Fast ===
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            // 'inherited' is nullable; do not throw when it's null — handle below.

            // === Prepare ===
            var entityMap = _mapService.GetMap().Entities;
            var tokenResult = dto.EngineResults.OfType<TokenEngineResult>().FirstOrDefault();

            // === Initialize ===
            string entityCode;
            Guid inheritedFrom;
            bool isDefinedLocally;
            bool isDefault;

            // === Source detection ===
            if (tokenResult is { HasEntityToken: true, Tokens: not null })
            {
                // Local definition — USE VALUE, not Key ("Entity")
                if (!tokenResult.Tokens.TryGetValue("Entity", out var entityCodeLocal) || entityCodeLocal == null)
                    throw new InvalidOperationException(
                        $"Token inconsistency for DTO {dto.Id}: HasEntity=true but no 'Entity' key in Tokens.");

                entityCode = (entityCodeLocal.Value ?? string.Empty).ToUpperInvariant(); // ✅ fixed
                inheritedFrom = Guid.Empty;
                isDefinedLocally = true;
                isDefault = false;
            }
            else if (inherited.HasValue && inherited.Value.Id != Guid.Empty)
            {
                // Inherited
                entityCode = (inherited.Value.Code ?? string.Empty).ToUpperInvariant();
                inheritedFrom = inherited.Value.Id;
                isDefinedLocally = false;
                isDefault = false;
            }
            else
            {
                // Default
                entityCode = "SDE";
                inheritedFrom = Guid.Empty;
                isDefinedLocally = false;
                isDefault = true;
            }

            // === Validate ===
            bool isValid = !string.IsNullOrWhiteSpace(entityCode) && entityMap.ContainsKey(entityCode);

            // === Relationship flags ===
            bool hasParent = inherited.HasValue && inherited.Value.Id != Guid.Empty;
            bool isInherited = !isDefinedLocally && !isDefault && hasParent;
            bool isForeign = isDefinedLocally
                               && hasParent
                               && !string.IsNullOrEmpty(inherited.Value.Code)
                               && !entityCode.Equals(inherited.Value.Code, StringComparison.OrdinalIgnoreCase);

            // === Message ===
            string message = isValid
                ? (isDefinedLocally
                    ? (isForeign
                        ? $"Entity defined locally and differs from parent ({inherited?.Code} -> {entityCode})."
                        : $"Entity defined locally: {entityCode}")
                    : (isInherited
                        ? $"Inherited Entity Code: {inherited?.Code} from {inherited?.Id}"
                        : $"Entity not inherited (no parent, defaulted to {entityCode})."))
                : $"Unknown entity detected ('{entityCode}'), fallback to SDE.";

            // If invalid, actually fall back to SDE so output is consistent with the message
            if (!isValid)
            {
                entityCode = "SDE";
                isDefault = true;
                isInherited = false;
            }

            // === Build result ===
            return new EntityEngineResult
            {
                SourceDtoId = dto.Id,
                Entity = entityCode,     // ✅ VALUE like "SDE" shows in logs/results
                IsValid = true,           // we normalize to a valid value (SDE) if needed
                IsInherited = isInherited,
                IsForeign = isForeign,
                IsDefault = isDefault,
                InheritedFrom = inheritedFrom,
                Message = new List<string>()
            }
            .Also(r => r.AddMessage(message));
        }
    }
}
