using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.Extensions;

namespace SMSgroup.Aveva.Utilities.Engines
{
    /// <summary>
    /// Determines the AVEVA role (WORL, SITE, SUB_SITE, ZONE, EQUI, STRU)
    /// based on token structure and discipline classification.
    /// </summary>
    public sealed class RoleEngine : IEngine
    {
        private readonly IRoleMapService _roleMapService;

        public RoleEngine(IRoleMapService roleMapService)
        {
            _roleMapService = roleMapService ?? throw new ArgumentNullException(nameof(roleMapService));
        }

        public RoleEngineResult Process(TakeOverPointDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var geomType = dto.GeometryType?.Trim()?.ToUpperInvariant() ?? string.Empty;
            var result = new RoleEngineResult
            {
                SourceDtoId = dto.Id,
                IsValid = !string.IsNullOrEmpty(geomType)
            };

            switch (geomType)
            {
                case "NOZZ":
                    result.AvevaType = "NOZZ";
                    result.IsLeaf = true;
                    result.AddMessage($"✅ GeometryType 'NOZZ' mapped to AVEVA type 'NOZZ' (leaf).");
                    break;

                case "ELCONN":
                    result.AvevaType = "ELCONN";
                    result.IsLeaf = true;
                    result.AddMessage($"✅ GeometryType 'ELCONN' mapped to AVEVA type 'ELCONN' (leaf).");
                    break;

                case "DATUM":
                    result.AvevaType = "DATUM";
                    result.IsLeaf = true;
                    result.AddMessage($"✅ GeometryType 'DATUM' mapped to AVEVA type 'DATUM' (leaf).");
                    break;

                default:
                    result.AvevaType = geomType;
                    result.IsLeaf = false;
                    result.AddMessage($"⚠️ GeometryType '{geomType}' not recognized. Treated as non-leaf.");
                    break;
            }

            return result;
        }

        /// <summary>
        /// Resolves the most probable AVEVA role for a DTO using tokens.
        /// </summary>
        public RoleEngineResult Process(ProjectStructureDTO dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var tokenResult = dto.EngineResults?.OfType<TokenEngineResult>().FirstOrDefault();
            if (tokenResult?.Tokens == null || tokenResult.Tokens.Count == 0)
            {
                return new RoleEngineResult
                {
                    SourceDtoId = dto.Id,
                    IsValid = false,
                    IsLeaf = false,
                    AvevaType = string.Empty
                }.Also(r => r.AddError("❌ Role not resolved (no token data)."));
            }

            var (role, isLeaf, note) = ResolveRole(tokenResult);

            return new RoleEngineResult
            {
                SourceDtoId = dto.Id,
                IsValid = !string.IsNullOrWhiteSpace(role),
                IsLeaf = isLeaf,
                AvevaType = role
            }.Also(r => r.AddMessage($"Resolved Role: {role}. {note}"));
        }
        private static (string Role, bool IsLeaf, string Note) ResolveRole(TokenEngineResult tr)
        {
            // Canonical base presence after your replacement model
            bool hasPlant = tr.Tokens.ContainsKey("Plant");
            bool hasUnit = tr.Tokens.ContainsKey("PlantUnit");
            bool hasSection = tr.Tokens.ContainsKey("PlantSection");
            bool hasEquipment = tr.Tokens.ContainsKey("Equipment");
            bool hasComponent = tr.Tokens.ContainsKey("Component");

            // Discipline (restricted to ST / CI / LA); default to LA if missing/unknown
            var discipline = tr.Tokens.TryGetValue("Discipline", out var dTok)
                ? (dTok.Value ?? "").Trim().ToUpperInvariant()
                : "LA";
            if (discipline is not ("ST" or "CI" or "LA"))
                discipline = "LA";

            // 1) WORL / SITE trivial ladders
            if (hasPlant && !hasUnit && !hasSection && !hasEquipment && !hasComponent)
                return ("WORL", false, "(Plant only)");
            if (hasPlant && hasUnit && !hasSection && !hasEquipment && !hasComponent)
                return ("SITE", false, "(Plant + PlantUnit)");

            // 2) Leaf always EQUI if Component is present
            if (hasComponent)
                return ("EQUI", true, "(Component present → leaf)");

            // 3) Equipment present (no Component) → ZONE
            if (hasPlant && hasUnit && hasSection && hasEquipment && !hasComponent)
                return ("ZONE", false, "(Section + Equipment, no Component)");

            // 4) Section present (no Equipment / Component) → SUB_SITE or ZONE by discipline
            if (hasPlant && hasUnit && hasSection && !hasEquipment && !hasComponent)
            {
                // ST/CI → ZONE; LA (or missing) → SUB_SITE
                var role = (discipline is "ST" or "CI") ? "ZONE" : "SUB_SITE";
                return (role, false, $"(Section present; discipline={discipline} → {role})");
            }

            // 5) Fallback by base count (rare / partially missing inputs)
            var baseCount = tr.Tokens.Values.Count(t => string.Equals(t.Type, "base", StringComparison.OrdinalIgnoreCase));
            return baseCount switch
            {
                1 => ("WORL", false, "(fallback by count)"),
                2 => ("SITE", false, "(fallback by count)"),
                3 => (discipline is "ST" or "CI" ? "ZONE" : "SUB_SITE", false, "(fallback by count + discipline)"),
                4 => ("ZONE", false, "(fallback by count)"),
                5 => ("EQUI", true, "(fallback by count)"),
                _ => (string.Empty, false, "(no match)")
            };
        }
    }
}
