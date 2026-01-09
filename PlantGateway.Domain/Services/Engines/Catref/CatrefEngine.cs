using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.ValueObjects;

namespace PlantGateway.Domain.Services.Engines.Catref
{
    public sealed class CatrefEngine : IEngine
    {
        private readonly ICatrefMapService _catrefMapService;

        public CatrefEngine(ICatrefMapService catrefMapService)
        {
            _catrefMapService = catrefMapService ?? throw new ArgumentNullException(nameof(catrefMapService));
        }

        public CatrefEngineResult Process(TakeOverPointDTO dto)
        {
            // Step 0 -- prepare result object
            var catrefEngineResult = new CatrefEngineResult();

            // Step 1 -- try get direct Catref from DTO
            var raw = TryUseRawCatref(dto);
            if (!string.IsNullOrWhiteSpace(raw))
            {
                catrefEngineResult.CatrefResolutionFlag = CatrefResolutionFlags.RawCatref;
                //catrefEngineResult.GeometryType = dto.GeometryType;
                catrefEngineResult.Catref = raw;
                return catrefEngineResult;
            }

            // Step 2 -- try construct key from DTO properties
            var constructed = TryConstructKey(dto);
            if (!string.IsNullOrWhiteSpace(constructed))
            {
                catrefEngineResult.CatrefResolutionFlag = CatrefResolutionFlags.ConstructKey;
                //catrefEngineResult.GeometryType = dto.GeometryType;
                catrefEngineResult.Catref = constructed;
                return catrefEngineResult;
            }

            // Step 3 -- try match from Csys-Description to CatrefMapDTO
            var fromDescToCatrefMapDTO = TryMatchFromDescriptionToCatrefMapDTO(dto);
            if (!string.IsNullOrWhiteSpace(fromDescToCatrefMapDTO))
            {
                catrefEngineResult.CatrefResolutionFlag = CatrefResolutionFlags.FromDesc;
                //catrefEngineResult.GeometryType = dto.GeometryType;
                catrefEngineResult.Catref = fromDescToCatrefMapDTO;
                return catrefEngineResult;
            }

            // Step 4 -- method to bypass SSAB LULEA project issue with Catalogues
            var fromDescToCYLI = DefineCYLI(dto);
            if (!string.IsNullOrWhiteSpace(fromDescToCYLI))
            {
                catrefEngineResult.CatrefResolutionFlag = CatrefResolutionFlags.UsedBypassCYLI;
                //catrefEngineResult.GeometryType = "CYLI";
                dto.EngineResults.OfType<RoleEngineResult>().FirstOrDefault().AvevaType = "CYLI"; // drama spagetti code
                catrefEngineResult.Catref = fromDescToCYLI;
                return catrefEngineResult;
            }

            // Step 5 -- fallback to default Catref for geometry type
            catrefEngineResult.CatrefResolutionFlag = CatrefResolutionFlags.UsedDefault;
            //catrefEngineResult.GeometryType = dto.GeometryType;
            catrefEngineResult.Catref = GetDefault(dto);
            return catrefEngineResult;
        }

        /// <summary>
        /// Step 1: Use RawCatref directly from DTO if present.
        /// </summary>
        public string TryUseRawCatref(TakeOverPointDTO dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.RawCatref))
            {
                Log("Step 1", $"Using RawCatref: {dto.RawCatref}");
                return dto.RawCatref.Trim();
            }

            Log("Step 1", "No RawCatref provided.");
            return string.Empty;
        }

        /// <summary>
        /// Step 2: Try to construct a Catref key from DTO properties
        /// (DN, PN, Norm, ConnectionType). Currently placeholder.
        /// </summary>
        public string TryConstructKey(TakeOverPointDTO dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.DN) ||
                !string.IsNullOrWhiteSpace(dto.PN) ||
                !string.IsNullOrWhiteSpace(dto.Norm) ||
                !string.IsNullOrWhiteSpace(dto.ConnectionType))
            {
                Log("Step 2", $"DTO fields present (DN={dto.DN}, PN={dto.PN}, Norm={dto.Norm}, Conn={dto.ConnectionType}) but mapping not implemented.");
            }
            else
            {
                Log("Step 2", "No DN/PN/Norm/ConnectionType data available.");
            }

            return string.Empty;
        }

        /// <summary>
        /// Step 3: Try to match a Catref key from the Csys-Description field.
        /// Performs normalization and exact dictionary match.
        /// </summary>
        public string TryMatchFromDescriptionToCatrefMapDTO(TakeOverPointDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Description) || string.IsNullOrWhiteSpace(dto.GeometryType))
            {
                Log("Step 3", "Description or GeometryType missing.");
                return string.Empty;
            }

            var map = GetMap(dto.GeometryType);
            if (map == null || map.Count == 0)
            {
                Log("Step 3", $"No Catref map found for GeometryType={dto.GeometryType}");
                return string.Empty;
            }

            // Normalize description (preserve underscores, unify case)
            var normalizedDesc = dto.Description
                .Replace('.', '_')
                .Replace('-', '_')
                .ToUpperInvariant();

            // Find candidates contained in description, order longest first
            var candidates = map.Keys
                .Where(k => !string.IsNullOrWhiteSpace(k) &&
                            normalizedDesc.Contains(k.ToUpperInvariant()))
                .OrderByDescending(k => k.Length)
                .ToList();

            Log("Step 3", $"Normalized Desc='{normalizedDesc}', Candidates=[{string.Join(", ", candidates)}]");

            if (candidates.Count >= 1)
            {
                var winner = candidates[0];
                Log("Step 3", $"Winner '{winner}' → {map[winner]}");
                return map[winner];
            }

            Log("Step 3", "No match found in description.");
            return string.Empty;
        }

        /// <summary>
        /// Step 3b: Bypass method – if no Catref found in the map,
        /// try to extract OD value from the Description and define a CYLI element.
        /// Rules:
        /// - OD value is just before 'X' or 'x'
        /// - Underscore '_' in numbers represents a decimal point
        /// - Example: "48_3X6_3" → OD = 48.3
        /// Returns a formatted string for CYLI element definition.
        /// </summary>
        public string DefineCYLI(TakeOverPointDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Description))
            {
                Log("Bypass", "Description missing – cannot define CYLI.");
                return string.Empty;
            }

            // Normalize description
            var desc = dto.Description.ToUpperInvariant();

            // Look for 'X' which separates OD and thickness
            int idx = desc.IndexOf('X');
            if (idx <= 0)
            {
                Log("Bypass", $"No 'X' found in description: {desc}");
                return string.Empty;
            }

            // Extract substring before 'X'
            var candidate = desc.Substring(0, idx);

            // Find the last numeric sequence in that part
            var digits = new string(candidate.Reverse()
                .TakeWhile(c => char.IsDigit(c) || c == '_')
                .Reverse()
                .ToArray());

            if (string.IsNullOrWhiteSpace(digits))
            {
                Log("Bypass", $"No numeric OD found in description: {desc}");
                return string.Empty;
            }

            // Replace '_' with '.' to get real number
            var odString = digits.Replace('_', '.');

            if (!double.TryParse(odString, System.Globalization.NumberStyles.Any,
                                 System.Globalization.CultureInfo.InvariantCulture, out var odValue))
            {
                Log("Bypass", $"Failed to parse OD='{odString}' from description: {desc}");
                return string.Empty;
            }

            // Build CYLI definition. Replace ',' with '.' for decimal point.
            string result = $"Diameter {odValue.ToString().Replace(',', '.')}mm Height 200mm";

            Log("Bypass", $"Generated CYLI → {result}");

            return result;
        }

        /// <summary>
        /// Step 4: Return the default Catref for the given geometry type.
        /// </summary>
        public string GetDefault(TakeOverPointDTO dto)
        {
            var map = GetMap(dto.GeometryType);
            if (map != null && map.TryGetValue("Default", out var defValue))
            {
                Log("Step 4", $"Using default Catref for {dto.GeometryType}: {defValue}");
                return defValue;
            }

            Log("Step 4", $"No default Catref available for {dto.GeometryType}");
            return string.Empty;
        }

        // --- Helpers ---

        private Dictionary<string, string>? GetMap(string geometryType)
        {
            var dto = _catrefMapService.GetMap();
            if (!Enum.TryParse<GeometryType>(geometryType, true, out var type))
                return null;

            return type switch
            {
                GeometryType.NOZZ => dto.Nozz,
                GeometryType.ELCONN => dto.Elconn,
                GeometryType.DATUM => dto.Datum,
                _ => null
            };
        }

        private void Log(string step, string message)
        {
            Console.WriteLine($"[CatrefEngine] {step} → {message}");
        }
    }
}
