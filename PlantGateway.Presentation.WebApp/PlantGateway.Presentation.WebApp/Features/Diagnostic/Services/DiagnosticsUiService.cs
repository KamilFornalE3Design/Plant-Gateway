using Microsoft.Extensions.Options;
using PlantGateway.Presentation.WebApp.Configuration.Options;
using PlantGateway.Presentation.WebApp.Features.Diagnostic.Models;
using System.Text.Json;

namespace PlantGateway.Presentation.WebApp.Features.Diagnostic.Services
{
    public class DiagnosticsUiService
    {
        private readonly string _storagePath;

        public DiagnosticsUiService(IWebHostEnvironment env, IOptions<DiagnosticsOptions> optionsAccessor)
        {
            var options = optionsAccessor.Value;

            _storagePath = Path.IsPathRooted(options.StoragePath)
                ? options.StoragePath
                : Path.Combine(env.ContentRootPath, options.StoragePath);
        }

        /// <summary>
        /// Loads a diagnostic JSON (new pipeline format) and builds a view model for the UI.
        /// </summary>
        public async Task<DiagnosticsViewModel?> LoadAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            var path = Path.IsPathRooted(fileName)
                ? fileName
                : Path.Combine(_storagePath, fileName);

            if (!File.Exists(path))
                return null;

            await using var stream = File.OpenRead(path);
            using var doc = await JsonDocument.ParseAsync(stream);

            var root = doc.RootElement;

            var vm = new DiagnosticsViewModel
            {
                Summary = BuildSummary(root, Path.GetFileName(path)),
                EngineSummaries = BuildEngineSummaries(root),
                MatrixRows = BuildMatrixRows(root),
                Messages = BuildMessages(root)
            };

            return vm;
        }

        public async Task<DiagnosticsViewModel?> LoadFromStreamAsync(Stream stream, string fileName)
        {
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            var vm = new DiagnosticsViewModel
            {
                Summary = BuildSummary(root, fileName),
                EngineSummaries = BuildEngineSummaries(root),
                MatrixRows = BuildMatrixRows(root),
                Messages = BuildMessages(root)
            };

            return vm;
        }


        // ─────────────────────────────────────────────────────────────
        // Builders for the new JSON format
        // ─────────────────────────────────────────────────────────────

        private static DiagnosticsRunSummary BuildSummary(JsonElement root, string fileName)
        {
            var summary = new DiagnosticsRunSummary
            {
                FileName = fileName
            };

            // ----- summary.warnings / summary.errors -----
            if (root.TryGetProperty("summary", out var summaryElement) &&
                summaryElement.ValueKind == JsonValueKind.Object)
            {
                int warningCount = 0;
                int errorCount = 0;

                if (summaryElement.TryGetProperty("warnings", out var warningsElem) &&
                    warningsElem.ValueKind == JsonValueKind.Array)
                {
                    warningCount = warningsElem.GetArrayLength();
                }

                if (summaryElement.TryGetProperty("errors", out var errorsElem) &&
                    errorsElem.ValueKind == JsonValueKind.Array)
                {
                    errorCount = errorsElem.GetArrayLength();
                }

                summary.WarningCount = warningCount;
                summary.ErrorCount = errorCount;
                summary.MessageCount = warningCount + errorCount;

                // Derive IsValid / IsSuccess from presence of errors
                summary.IsValid = errorCount == 0;
                summary.IsSuccess = errorCount == 0;
            }

            // ----- quality (root.quality) -----
            if (root.TryGetProperty("quality", out var quality) &&
                quality.ValueKind == JsonValueKind.Object)
            {
                if (quality.TryGetProperty("totalScore", out var total))
                    summary.TotalScore = total.GetDouble();
                if (quality.TryGetProperty("syntaxScore", out var syntax))
                    summary.SyntaxScore = syntax.GetDouble();
                if (quality.TryGetProperty("semanticScore", out var sem))
                    summary.SemanticScore = sem.GetDouble();
                if (quality.TryGetProperty("completenessScore", out var comp))
                    summary.CompletenessScore = comp.GetDouble();
                if (quality.TryGetProperty("normalizationScore", out var norm))
                    summary.NormalizationScore = norm.GetDouble();
            }

            // ----- details.parser (schema, sourceSystem, parserHints) -----
            if (root.TryGetProperty("details", out var details) &&
                details.ValueKind == JsonValueKind.Object &&
                details.TryGetProperty("parser", out var parser) &&
                parser.ValueKind == JsonValueKind.Object)
            {
                if (parser.TryGetProperty("detectedInputSchema", out var schema))
                    summary.DetectedInputSchema = schema.GetString() ?? string.Empty;

                if (parser.TryGetProperty("sourceSystem", out var src))
                    summary.SourceSystem = src.GetString() ?? string.Empty;

                if (parser.TryGetProperty("parserHints", out var hints) &&
                    hints.ValueKind == JsonValueKind.Object)
                {
                    if (hints.TryGetProperty("totalNodeCount", out var nodes))
                        summary.TotalNodeCount = nodes.GetInt32();
                    if (hints.TryGetProperty("totalPartNodeCount", out var parts))
                        summary.TotalPartNodeCount = parts.GetInt32();
                }
            }

            return summary;
        }

        private static List<EngineSummary> BuildEngineSummaries(JsonElement root)
        {
            var list = new List<EngineSummary>();

            if (!root.TryGetProperty("summary", out var summary) ||
                summary.ValueKind != JsonValueKind.Object)
                return list;

            if (!summary.TryGetProperty("engines", out var enginesElement) ||
                enginesElement.ValueKind != JsonValueKind.Object)
                return list;

            foreach (var prop in enginesElement.EnumerateObject())
            {
                var engineObj = prop.Value;
                if (engineObj.ValueKind != JsonValueKind.Object)
                    continue;

                var es = new EngineSummary
                {
                    Name = prop.Name
                };

                if (engineObj.TryGetProperty("ok", out var ok))
                    es.Ok = ok.GetInt32();
                if (engineObj.TryGetProperty("missing", out var missing))
                    es.Missing = missing.GetInt32();

                list.Add(es);
            }

            return list;
        }

        private static List<EngineMatrixRow> BuildMatrixRows(JsonElement root)
        {
            var rows = new List<EngineMatrixRow>();

            if (!root.TryGetProperty("details", out var details) ||
                details.ValueKind != JsonValueKind.Object)
                return rows;

            if (!details.TryGetProperty("dtos", out var dtos) ||
                dtos.ValueKind != JsonValueKind.Array)
                return rows;

            foreach (var dto in dtos.EnumerateArray())
            {
                if (dto.ValueKind != JsonValueKind.Object)
                    continue;

                var row = new EngineMatrixRow
                {
                    DtoName = dto.TryGetProperty("dtoName", out var nameElem)
                        ? (nameElem.GetString() ?? string.Empty)
                        : string.Empty
                };

                if (!dto.TryGetProperty("engines", out var engines) ||
                    engines.ValueKind != JsonValueKind.Array)
                {
                    rows.Add(row);
                    continue;
                }

                foreach (var engine in engines.EnumerateArray())
                {
                    if (engine.ValueKind != JsonValueKind.Object)
                        continue;

                    var engineName = engine.TryGetProperty("type", out var typeElem)
                        ? (typeElem.GetString() ?? string.Empty)
                        : string.Empty;

                    if (string.IsNullOrWhiteSpace(engineName))
                        continue;

                    int errorCount = 0;
                    int warningCount = 0;

                    if (engine.TryGetProperty("error", out var errElem) &&
                        errElem.ValueKind == JsonValueKind.Array)
                    {
                        errorCount = errElem.GetArrayLength();
                    }

                    if (engine.TryGetProperty("warning", out var warnElem) &&
                        warnElem.ValueKind == JsonValueKind.Array)
                    {
                        warningCount = warnElem.GetArrayLength();
                    }

                    bool isValid = engine.TryGetProperty("isValid", out var validElem) &&
                                   validElem.ValueKind == JsonValueKind.True;

                    EngineCellStatus status;

                    if (errorCount > 0)
                        status = EngineCellStatus.Error;
                    else if (warningCount > 0)
                        status = EngineCellStatus.Warning;
                    else if (isValid)
                        status = EngineCellStatus.Ok;
                    else
                        status = EngineCellStatus.Missing;

                    row.Engines[engineName] = status;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static List<DiagnosticMessage> BuildMessages(JsonElement root)
        {
            var list = new List<DiagnosticMessage>();

            if (!root.TryGetProperty("summary", out var summary) ||
                summary.ValueKind != JsonValueKind.Object)
                return list;

            // helper local function
            static void AddMessages(JsonElement arr, string defaultSeverity, List<DiagnosticMessage> target)
            {
                if (arr.ValueKind != JsonValueKind.Array)
                    return;

                foreach (var w in arr.EnumerateArray())
                {
                    if (w.ValueKind != JsonValueKind.Object)
                        continue;

                    var msg = new DiagnosticMessage
                    {
                        Severity = defaultSeverity
                    };

                    if (w.TryGetProperty("dtoName", out var dtoName))
                        msg.DtoName = dtoName.GetString() ?? string.Empty;

                    if (w.TryGetProperty("engine", out var eng))
                        msg.Engine = eng.GetString() ?? string.Empty;

                    if (w.TryGetProperty("message", out var text))
                        msg.Message = text.GetString() ?? string.Empty;

                    if (w.TryGetProperty("link", out var link))
                        msg.Link = link.GetString();

                    // If "severity" property exists in JSON, prefer it.
                    if (w.TryGetProperty("severity", out var sev) &&
                        sev.ValueKind == JsonValueKind.String)
                    {
                        var value = sev.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            msg.Severity = value;
                    }

                    target.Add(msg);
                }
            }

            if (summary.TryGetProperty("warnings", out var warningsElem))
                AddMessages(warningsElem, "warning", list);

            if (summary.TryGetProperty("errors", out var errorsElem))
                AddMessages(errorsElem, "error", list);

            return list;
        }
    }
}
