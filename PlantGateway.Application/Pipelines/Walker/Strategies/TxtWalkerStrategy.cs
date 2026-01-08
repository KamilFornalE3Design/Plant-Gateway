using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Attributes;
using SMSgroup.Aveva.Config.Data.IdentityCache;
using SMSgroup.Aveva.Config.Mappers;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Walker;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using PlantGateway.Application.Pipelines.Engines;
using PlantGateway.Application.Pipelines.Walker.Interfaces;

namespace PlantGateway.Application.Pipelines.Walker.Strategies
{
    /// <summary>
    /// Strategy for walking TXT ASM files into TakeOverPointDTOs.
    /// Resolves headers dynamically using IHeaderMapService and HeaderMapDTO.
    /// Populates RawItems in the provided PipelineContract.
    /// </summary>
    public sealed class TxtWalkerStrategy : IWalkerStrategy<TakeOverPointDTO>
    {
        private readonly IConfigProvider _configProvider;
        private readonly IHeaderMapService _headerMapService;
        private readonly ICatrefMapService _catrefMapService;

        private readonly IdentityEngine _identityEngine;
        private readonly TakeOverPointCacheService _cacheService;

        public TxtWalkerStrategy(IConfigProvider configProvider, IHeaderMapService headerMapService, ICatrefMapService catrefMapService, TakeOverPointCacheService cacheService)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _headerMapService = headerMapService ?? throw new ArgumentNullException(nameof(headerMapService));
            _catrefMapService = catrefMapService ?? throw new ArgumentNullException(nameof(catrefMapService));

            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _identityEngine = new IdentityEngine(_cacheService);
        }

        public InputDataFormat Format => InputDataFormat.txt;

        public WalkerResult<TakeOverPointDTO> Walk(PipelineContract<TakeOverPointDTO> pipelineContract)
        {
            if (pipelineContract == null)
                throw new ArgumentNullException(nameof(pipelineContract));

            var result = new WalkerResult<TakeOverPointDTO>
            {
                FilePath = pipelineContract.Input?.FilePath ?? string.Empty,
                ParserHints = pipelineContract.ParserResult?.ParserHints ?? new Dictionary<string, object>(),
                IsSuccess = false
            };

            try
            {
                // === STEP 1: Load + Normalize Separators ===
                result.RawLines = LoadRawLines(result.FilePath);
                result.RawLines = NormalizeSeparators(result.RawLines);

                // === STEP 2: Merge Prefixed Columns ===
                result.RawLines = MergePrefixedColumns(
                    result.RawLines,
                    result.ParserHints,
                    result.Warnings,
                    result.Modifications);

                // === STEP 3: Convert to Table ===
                var table = ConvertToTable(result.RawLines);
                result.MappedRows = table;

                // === STEP 4: Map DTOs ===
                var headerMap = _headerMapService.GetMap();
                var dtos = CreateDtos(table, pipelineContract, headerMap);

                result.Dtos.AddRange(dtos);

                pipelineContract.Items = result.Dtos;

                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"❌ Walker failed: {ex.Message}");
                result.IsSuccess = false;
            }

            return result;
        }

        // ==========================================================
        // STEP 1 – Load File
        // ==========================================================
        private static List<string> LoadRawLines(string filePath)
        {
            var lines = new List<string>();
            using var reader = new StreamReader(filePath);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line != null)
                    lines.Add(line);
            }
            return lines;
        }

        private static List<string> NormalizeSeparators(List<string> rawLines)
        {
            var normalized = new List<string>(rawLines.Count);
            foreach (var line in rawLines)
            {
                if (line == null) continue;
                var normalizedLine = line.Replace("\t", "|").TrimEnd('\r', '\n');
                normalized.Add(normalizedLine);
            }
            return normalized;
        }

        // ==========================================================
        // STEP 2 – Merge Prefixed Columns (generic)
        // ==========================================================
        private List<string> MergePrefixedColumns(List<string> rawLines, Dictionary<string, object> parserHints, List<string> warnings, Dictionary<WalkerModificationType, List<string>> modifications)
        {
            if (rawLines == null || rawLines.Count == 0)
                return new List<string>();

            var merged = new List<string>(rawLines.Count);

            // 1️⃣ Detect header using the RowMapper (domain-driven)
            string[] rawHeaders;
            try
            {
                var headerMap = _headerMapService.GetMap().Headings;
                var rowMapper = new TakeOverPointRowMapper(headerMap, parserHints);
                rawHeaders = rowMapper.DetectHeaders(rawLines, new HeaderMapDTO { Headings = headerMap });
            }
            catch (Exception ex)
            {
                warnings.Add($"⚠️ Failed to detect headers: {ex.Message}");
                return rawLines;
            }

            // Find the actual header line in input
            var headerLine = rawLines.FirstOrDefault(line =>
                rawHeaders.Any(h => line.Contains(h, StringComparison.OrdinalIgnoreCase)));

            if (string.IsNullOrWhiteSpace(headerLine))
            {
                warnings.Add("⚠️ Could not locate the detected header line in raw input.");
                return rawLines;
            }

            // Detect delimiter dynamically
            var delimiter = DetectDelimiter(headerLine);
            var headers = headerLine.TrimStart(';').Split(delimiter).Select(h => h.Trim()).ToArray();

            // 2️⃣ Find duplicate logical columns (same suffix)
            var headerGroups = headers
                .Select((h, idx) => new { Header = h, Index = idx })
                .GroupBy(x => GetSuffix(x.Header))
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 3️⃣ Preference order (can be overridden via ParserHints["MergePreferences"])
            var prefOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Owner-Comp-Param"] = 1,
                ["Csys-Feat-Param"] = 2
            };

            if (parserHints.TryGetValue("MergePreferences", out var prefObj) && prefObj is Dictionary<string, int> cfg)
            {
                foreach (var kv in cfg)
                    prefOrder[kv.Key] = kv.Value;
            }

            // 4️⃣ Build new header structure
            var newHeaders = headers.ToList();
            foreach (var kv in headerGroups)
            {
                var suffix = kv.Key;
                var duplicates = kv.Value.Select(x => x.Header).ToList();

                // Remove all duplicates from header, then add one unified suffix
                newHeaders.RemoveAll(h => duplicates.Contains(h));
                newHeaders.Add(suffix);

                AddModification(modifications, WalkerModificationType.Merge,
                    $"Header merge: {suffix} ← {string.Join(", ", duplicates)}");
            }

            merged.Add(";" + string.Join("|", newHeaders));

            // 5️⃣ Merge data rows
            int rowIndex = 1;
            foreach (var line in rawLines.SkipWhile(l => !l.Contains(rawHeaders.First(), StringComparison.OrdinalIgnoreCase)).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
                    continue;

                var cells = line.Split(delimiter);
                var rowDict = headers.Zip(cells, (h, v) => new { h, v }).ToDictionary(x => x.h, x => x.v);

                foreach (var kv in headerGroups)
                {
                    var suffix = kv.Key;
                    var candidates = kv.Value.Select(x => x.Header).ToList();
                    var ordered = candidates
                        .OrderBy(h => prefOrder.TryGetValue(GetPrefix(h), out var rank) ? rank : int.MaxValue)
                        .ToList();

                    string mergedValue = string.Empty;
                    string? sourceTaken = null;

                    foreach (var h in ordered)
                    {
                        if (rowDict.TryGetValue(h, out var val) && !string.IsNullOrWhiteSpace(val))
                        {
                            mergedValue = val.Trim();
                            sourceTaken = h;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(mergedValue))
                    {
                        warnings.Add($"[Merge] No data for {suffix} on line {rowIndex}");
                        AddModification(modifications, WalkerModificationType.Merge,
                            $"Row {rowIndex}: Empty sources for {suffix} → assigned ''");
                    }
                    else if (ordered.Count > 1 && !string.IsNullOrEmpty(sourceTaken)
                             && !string.Equals(sourceTaken, ordered.First(), StringComparison.OrdinalIgnoreCase))
                    {
                        AddModification(modifications, WalkerModificationType.Merge,
                            $"Row {rowIndex}: Multiple values for {suffix} → took {sourceTaken}");
                    }

                    rowDict[suffix] = mergedValue;
                }

                var newRow = string.Join("|", newHeaders.Select(h => rowDict.TryGetValue(h, out var v) ? v : string.Empty));
                merged.Add(newRow);
                rowIndex++;
            }

            return merged;
        }

        private static string GetPrefix(string header)
        {
            var idx = header.IndexOf(':');
            return idx > 0 ? header.Substring(0, idx).Trim() : string.Empty;
        }

        private static string GetSuffix(string header)
        {
            var idx = header.IndexOf(':');
            return idx > 0 ? header.Substring(idx + 1).Trim() : header.Trim();
        }
        private static void AddModification(Dictionary<WalkerModificationType, List<string>> modifications, WalkerModificationType type, string message)
        {
            if (!modifications.TryGetValue(type, out var list))
            {
                list = new List<string>();
                modifications[type] = list;
            }

            list.Add(message);
        }


        // ==========================================================
        // STEP 3 – Convert to Table
        // ==========================================================
        private static List<Dictionary<string, string>> ConvertToTable(List<string> lines)
        {
            var table = new List<Dictionary<string, string>>();
            if (lines.Count == 0)
                return table;

            var headerLine = lines.FirstOrDefault(l => l.StartsWith(";", StringComparison.OrdinalIgnoreCase));
            if (headerLine == null)
                return table;

            var delimiter = DetectDelimiter(headerLine);
            var headers = headerLine.TrimStart(';')
                .Split(delimiter)
                .Select(h => h.Trim())
                .ToArray();

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
                    continue;

                var parts = line.Split(delimiter);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < headers.Length; i++)
                {
                    var key = headers[i];
                    var value = i < parts.Length ? parts[i].Trim() : string.Empty;
                    row[key] = value;
                }

                table.Add(row);
            }

            return table;
        }
        private static char DetectDelimiter(string headerLine)
        {
            if (headerLine.Contains('|')) return '|';
            if (headerLine.Contains('\t')) return '\t';
            if (headerLine.Contains(';')) return ';';
            return ' '; // fallback (space-separated)
        }

        // ==========================================================
        // STEP 4 – Map DTOs
        // ==========================================================
        private IEnumerable<TakeOverPointDTO> CreateDtos(List<Dictionary<string, string>> table, PipelineContract<TakeOverPointDTO> pipelineContract, HeaderMapDTO headerMap)
        {
            var dtos = new List<TakeOverPointDTO>();
            if (table == null || table.Count == 0)
                return dtos;

            foreach (var row in table)
            {
                var dto = new TakeOverPointDTO();

                // === Map all properties that have HeaderKey attributes ===
                foreach (var prop in typeof(TakeOverPointDTO).GetProperties())
                {
                    var headerAttr = prop.GetCustomAttributes(typeof(HeaderKeyAttribute), false)
                                         .FirstOrDefault() as HeaderKeyAttribute;
                    if (headerAttr == null)
                        continue;

                    if (!headerMap.Headings.TryGetValue(headerAttr.Key, out var rawHeader))
                        continue;

                    string value = string.Empty;

                    if (row.TryGetValue(rawHeader, out var v))
                    {
                        value = v?.Trim() ?? string.Empty;
                    }
                    else
                    {
                        var prefixedMatch = row.FirstOrDefault(kv =>
                            kv.Key.EndsWith(rawHeader, StringComparison.OrdinalIgnoreCase));
                        if (!string.IsNullOrEmpty(prefixedMatch.Value))
                            value = prefixedMatch.Value.Trim();
                    }

                    prop.SetValue(dto, value);
                }

                // === Assign additional non-mapped properties ===
                // Assign Version to DTO from InputTarget File. It is not perfect solution but for otday need already something.
                dto.Version = pipelineContract.Input.Version;

                // no identity / no naming logic here
                dtos.Add(dto);
            }

            return dtos;
        }
    }
}
