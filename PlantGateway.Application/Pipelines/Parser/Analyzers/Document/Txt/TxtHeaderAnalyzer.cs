using SMSgroup.Aveva.Config.Mappers;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using SMSgroup.Aveva.Config.Models.ValueObjects;

namespace PlantGateway.Application.Pipelines.Parser.Analyzers.Document.Txt
{
    internal static class TxtHeaderAnalyzer
    {
        public static ParserResult Analyze(string path, HeaderMapDTO headerMap)
        {
            var result = new ParserResult
            {
                DetectedInputSchema = DetectedInputSchema.Unknown,
                SourceSystem = SourceSystem.Unknown,
                Summary = "No recognizable TakeOverPoint header detected.",
                TargetDtoType = new TakeOverPointDTO()
            };

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                result.Warnings.Add($"❌ File not found: {path}");
                return result;
            }

            var lines = File.ReadLines(path).ToList();

            // 1️ Detect headers (legacy or extended)
            var rowMapper = new TakeOverPointRowMapper(
                headerMap: headerMap.Headings,
                parserHints: result.ParserHints
            );

            string[]? headers;
            try
            {
                headers = rowMapper.DetectHeaders(lines, headerMap);
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"⚠ Header detection failed: {ex.Message}");
                return result;
            }

            // Normalize
            headers = headers.Select(h => h.Trim()).ToArray();

            // Load prefixes dynamically from map (if defined)
            headerMap.Groups.TryGetValue("HeaderPrefixes", out var prefixes);
            prefixes ??= Array.Empty<string>();

            bool hasAvevaTag = headers.Any(h => string.Equals(h, headerMap.Headings["AvevaTag"], StringComparison.OrdinalIgnoreCase));
            bool hasAvevaGeomRep = headers.Any(h => string.Equals(h, headerMap.Headings["GeometryType"], StringComparison.OrdinalIgnoreCase));

            // prefix-based extended headers
            bool hasPrefixedTag = headers.Any(h => prefixes.Any(p => h.StartsWith(p) && h.EndsWith(headerMap.Headings["AvevaTag"], StringComparison.OrdinalIgnoreCase)));
            bool hasPrefixedGeom = headers.Any(h => prefixes.Any(p => h.StartsWith(p) && h.EndsWith(headerMap.Headings["GeometryType"], StringComparison.OrdinalIgnoreCase)));

            string headerVersion;
            string summary;

            // ───── Scenario 1: no tag ─────
            if (!hasAvevaTag && !hasPrefixedTag)
            {
                headerVersion = "None";
                summary = "No AVEVA_TAG header detected — input is likely not a valid TakeOverPoint export.";
            }
            // ───── Scenario 2: legacy/mid ─────
            else if (hasAvevaTag && !hasPrefixedTag)
            {
                headerVersion = hasAvevaGeomRep ? "Mid" : "Old";
                summary = hasAvevaGeomRep
                    ? "Detected intermediate TakeOverPoint layout (AVEVA_TAG + AVEVA_GEOMREP)."
                    : "Detected legacy TakeOverPoint layout with single AVEVA_TAG column.";
            }
            // ───── Scenario 3: new extended ─────
            else if (hasPrefixedTag && hasPrefixedGeom)
            {
                headerVersion = "New";
                summary = "Detected modern TakeOverPoint layout with prefixed Owner/Csys-Feat AVEVA headers.";
            }
            else
            {
                headerVersion = "Unknown";
                summary = "Header structure is mixed or does not match known TOP patterns.";
            }

            if (headerVersion == "None" && headerVersion == "Unknown") result.Warnings.Add($"Unknown Header Version of {path}");
            result.DetectedInputSchema = DetectedInputSchema.TOP;
            result.SourceSystem = DetectSourceSystem(lines);
            result.Summary = summary;


            // ───────────────────────────────
            // Detect unexpected headers
            var validBaseHeaders = headerMap.Headings.Values.ToList();

            // Include all group-defined custom headers (like POSITION, ORIENTATION)
            var customGroupHeaders = headerMap.Groups
                .Where(g => !string.Equals(g.Key, "HeaderPrefixes", StringComparison.OrdinalIgnoreCase))
                .SelectMany(g => g.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .ToList();

            // Combine all valid base + custom headers
            var validRawHeaders = validBaseHeaders
                .Concat(customGroupHeaders)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Normalize prefixes (strip or ensure colon consistency)
            var prefixVariants = prefixes
                .SelectMany(p => new[]
                {
                    p.TrimEnd(':'), // "Owner-Comp-Param"
                    p.EndsWith(":") ? p : $"{p}:" // "Owner-Comp-Param:"
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Generate valid prefixed headers with both colon forms supported
            var validPrefixedHeaders = prefixVariants
                .SelectMany(prefix => validRawHeaders.Select(baseHeader => $"{prefix} {baseHeader}"))
                .ToList();

            // Merge everything into a single HashSet for lookup
            var allValidHeaders = validRawHeaders
                .Concat(validPrefixedHeaders)
                .Select(h => h.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Detect any unexpected headers
            var unexpectedHeaders = headers
                .Where(h => !allValidHeaders.Contains(h))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Add results to ParserResult
            if (unexpectedHeaders.Any())
            {
                result.UnexpectedHeaders.AddRange(unexpectedHeaders);
                result.Warnings.Add($"⚠ Unexpected headers detected: {string.Join(", ", unexpectedHeaders)}");
            }
            // ───────────────────────────────

            result.ParserHints = new Dictionary<string, object>
            {
                ["HeaderVersion"] = headerVersion,
                ["HasAvevaTag"] = hasAvevaTag,
                ["HasAvevaGeomRep"] = hasAvevaGeomRep,
                ["HasPrefixedTag"] = hasPrefixedTag,
                ["HasPrefixedGeom"] = hasPrefixedGeom,
                ["Prefixes"] = prefixes,
                ["RawHeaders"] = headers
            };

            return result;
        }

        private static SourceSystem DetectSourceSystem(IEnumerable<string> lines)
        {
            var topLines = lines.Take(10).ToList();

            if (topLines.Any(l => l.Contains("Creo", StringComparison.OrdinalIgnoreCase)))
                return SourceSystem.CreoParametric;
            if (topLines.Any(l => l.Contains("Inventor", StringComparison.OrdinalIgnoreCase)))
                return SourceSystem.Inventor;

            return SourceSystem.Unknown;
        }
    }
}
