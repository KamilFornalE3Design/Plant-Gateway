using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.Extensions;

namespace SMSgroup.Aveva.Utilities.Engines
{
    /// <summary>
    /// Resolves the intended Discipline for a single DTO.
    /// Validation and inheritance control are handled by the caller (strategy).
    /// </summary>
    public sealed class DisciplineEngine : IEngine
    {
        private readonly IDisciplineMapService _mapService;

        public DisciplineEngine(IDisciplineMapService mapService)
        {
            _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
        }

        public DisciplineEngineResult Process(TakeOverPointDTO dto)
        {
            var result = new DisciplineEngineResult
            {

            };

            return result;
        }

        /// <summary>
        /// Processes a single DTO using its TokenEngineResult and optional inherited discipline.
        /// </summary>
        public DisciplineEngineResult Process(ProjectStructureDTO dto, (Guid Id, string Code)? inherited)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            if (inherited == null)
                throw new ArgumentNullException(nameof(inherited));

            var map = _mapService.GetMap().Disciplines;
            var tokenEngineResult = dto.EngineResults.OfType<TokenEngineResult>().FirstOrDefault()
                        ?? throw new InvalidOperationException("❌ TokenEngineResult missing before DisciplineEngine.");

            // --- prepare local helpers ---
            Guid inheritedFrom = inherited.Value.Id;
            string inheritedCode = inherited.Value.Code ?? "ME";

            string disciplineCode;
            bool isDefinedLocally = false;
            bool isDefault = false;

            // === Decision chain in order of importance ===
            var decisions = new List<Func<string>>
            {
                // #1 Explicit discipline token
                () => tokenEngineResult.Tokens.TryGetValue("Discipline", out var token)
                ? token.Value.ToUpperInvariant()
                : string.Empty,

                // #2 Mechanical context
                () => tokenEngineResult.HasMechanicalToken ? "ME" : string.Empty,
            
                // #3 Civil context
                () => tokenEngineResult.HasCivilToken ? "CI" : string.Empty,
            
                // #4 Structural context
                () => tokenEngineResult.HasStructuralToken ? "ST" : string.Empty,
            
                // #5 Electrical context
                () => tokenEngineResult.HasElectricalToken ? "EA" : string.Empty,
            
                // #6 Piping context
                () => tokenEngineResult.HasPipingToken ? "PI" : string.Empty,
            
                // #7 Inherited discipline
                () => inheritedFrom != Guid.Empty ? inheritedCode.ToUpperInvariant() : string.Empty,
            
                // #8 Default
                () => "ME"
            };

            // === Evaluate chain with LINQ ===
            disciplineCode = decisions
                .Select(decide => decide())
                .FirstOrDefault(result => !string.IsNullOrEmpty(result))
                ?? string.Empty;

            // --- classify how it was set ---
            isDefinedLocally =
                tokenEngineResult.HasDisciplineToken ||
                tokenEngineResult.HasStructuralToken ||
                tokenEngineResult.HasCivilToken ||
                tokenEngineResult.HasElectricalToken ||
                tokenEngineResult.HasPipingToken;

            isDefault = disciplineCode.Equals("ME", StringComparison.OrdinalIgnoreCase) && inheritedFrom == Guid.Empty;

            bool isValid = map.ContainsKey(disciplineCode);
            bool hasParent = inheritedFrom != Guid.Empty;
            bool isInherited = !isDefinedLocally && !isDefault && hasParent;
            bool isForeign = isDefinedLocally &&
                             !string.IsNullOrEmpty(inheritedCode) &&
                             !disciplineCode.Equals(inheritedCode, StringComparison.OrdinalIgnoreCase);

            string message = isValid
                ? (isDefinedLocally
                    ? (isForeign
                        ? $"Discipline locally differs ({inheritedCode} → {disciplineCode})."
                        : $"Discipline defined locally: {disciplineCode}.")
                    : (isInherited
                        ? $"Inherited discipline: {disciplineCode}."
                        : $"Defaulted discipline: {disciplineCode}."))
                : "❌ Unknown discipline, fallback to ME.";

            return new DisciplineEngineResult
            {
                SourceDtoId = dto.Id,
                Discipline = disciplineCode,
                IsValid = isValid,
                IsInherited = isInherited,
                IsForeign = isForeign,
                IsDefault = isDefault,
                InheritedFrom = inheritedFrom,
                Message = new List<string>() // initialize as empty list
            }.Also(r => r.AddMessage(message))
                .When(r => !isValid, r => r.AddWarning("Discipline validity check failed."))
                .When(r => isForeign, r => r.AddWarning($"Discipline differs from inherited value '{inheritedCode}'."));
        }
    }
}
