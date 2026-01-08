using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using SMSgroup.Aveva.Utilities.Parser.Analyzers.Document.Txt;
using SMSgroup.Aveva.Utilities.Parser.Interfaces;
using System.Text.RegularExpressions;

namespace PlantGateway.Application.Pipelines.Parser.Strategies
{
    /// <summary>
    /// Parser strategy responsible for analyzing .txt inputs (TOP, etc.)
    /// and producing a preliminary ParserResult in pluggable phases.
    /// </summary>
    public sealed class TxtParserStrategy : IParserStrategy<TakeOverPointDTO>
    {
        // ==========================================================
        // ==============  FIELDS AND CONSTRUCTOR  ==================
        // ==========================================================

        #region Fields and Constructor

        private readonly IHeaderMapService _headerMapService;

        private string _path = string.Empty;
        private HeaderMapDTO _headerMap = new HeaderMapDTO();

        public InputDataFormat Format => InputDataFormat.txt;

        public TxtParserStrategy(IHeaderMapService headerMapService)
        {
            _headerMapService = headerMapService ?? throw new ArgumentNullException(nameof(headerMapService));
        }

        #endregion

        // ==========================================================
        // ===============  PUBLIC ENTRY POINTS  ====================
        // ==========================================================

        #region Public APIs

        public ParserResult Analyze(IPipelineContract pipeline)
        {
            if (pipeline is PipelineContract<ProjectStructureDTO> typedProjectStructure)
                return Analyze(typedProjectStructure);

            if (pipeline is PipelineContract<TakeOverPointDTO> typedTakeOverPoint)
                return Analyze(typedTakeOverPoint);

            throw new NotSupportedException("TxtParserStrategy only supports TakeOverPointDTO and ProjectStructureDTO at this time.");
        }

        public ParserResult Analyze(PipelineContract<ProjectStructureDTO> pipeline)
        {
            return new ParserResult();
        }

        public ParserResult Analyze(PipelineContract<TakeOverPointDTO> pipeline)
        {
            PreProcess(pipeline);

            var result = Process();

            PostProcess(pipeline, result);

            return result;
        }

        #endregion

        // ==========================================================
        // ===============  PRIVATE PHASES  =========================
        // ==========================================================

        #region PreProcess

        /// <summary>
        /// Validates the input file and loads the header map.
        /// Stores state in private fields for later use.
        /// </summary>
        private void PreProcess(PipelineContract<TakeOverPointDTO> pipeline)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));

            if (pipeline.Input == null || string.IsNullOrWhiteSpace(pipeline.Input.FilePath))
                throw new ArgumentException("PipelineContract.Input.Path cannot be null or empty.");

            _path = pipeline.Input.FilePath;
            if (!File.Exists(_path))
                throw new FileNotFoundException("Input TXT file not found.", _path);

            _headerMap = _headerMapService.GetMap();
        }

        #endregion


        #region Process

        /// <summary>
        /// Executes main parser logic: version detection and header analysis.
        /// </summary>
        private ParserResult Process()
        {
            // Detect version first (fast)
            var version = DetectVersion(_path);

            // Perform header analysis using the restored utility
            var result = AnalyzeHeaders(_path, _headerMap);

            // Enrich parser result with detected version
            result.Version = version;
            result.TargetDtoType = new TakeOverPointDTO();

            return result;
        }

        /// <summary>
        /// Executes the lightweight header analysis phase.
        /// Restored to its original utility-style form.
        /// </summary>
        private ParserResult AnalyzeHeaders(string path, HeaderMapDTO headerMap)
        {
            return TxtHeaderAnalyzer.Analyze(path, headerMap);
        }

        #endregion


        #region PostProcess

        /// <summary>
        /// Assigns discovered parser data to the pipeline contract and input.
        /// </summary>
        private void PostProcess(PipelineContract<TakeOverPointDTO> pipeline, ParserResult result)
        {
            pipeline.ParserResult = result;
            pipeline.Input.Version = result.Version;
        }

        #endregion


        // ==========================================================
        // ===============  LOW-LEVEL UTILITIES  ====================
        // ==========================================================

        #region DetectVersion

        private static string DetectVersion(string filePath)
        {
            // Step 1: Try from file name first (fast)
            var version = DetectVersionFromFileName(filePath);
            if (!string.IsNullOrEmpty(version))
                return version;

            // Step 2: Fallback to content scan
            return DetectVersionFromContent(filePath);
        }

        private static string DetectVersionFromFileName(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            // Step 1: find everything between ".asm" and ".txt"
            var outerMatch = Regex.Match(fileName, @"\.asm(.*?)\.txt", RegexOptions.IgnoreCase);
            if (outerMatch.Success)
            {
                var inner = outerMatch.Groups[1].Value?.Trim();

                // If ".asm.txt" → no version section, skip
                if (string.IsNullOrEmpty(inner))
                    return string.Empty;

                // Step 2: inside that value, look for known pattern "-<number>"
                var innerMatch = Regex.Match(inner, @"-(\d+)");
                if (innerMatch.Success)
                    return "-." + innerMatch.Groups[1].Value; // known pattern → "-.<number>"

                // If there is content but no pattern (future unknown format)
                // just return empty to let the pipeline fall back to content detection
                return string.Empty;
            }

            // No .asm segment at all
            return string.Empty;
        }

        private static string DetectVersionFromContent(string filePath)
        {
            try
            {
                const int scanLimit = 20;

                // Step 1: load limited number of lines
                var lines = File.ReadLines(filePath)
                                .Take(scanLimit)
                                .Select(l => l.Trim())
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .ToList();

                if (lines.Count == 0)
                    return string.Empty;

                // Step 2: find the first line containing "Version"
                var targetLine = lines.FirstOrDefault(
                    l => l.Contains("Version", StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrWhiteSpace(targetLine))
                    return string.Empty;

                // Step 3: clean the line from leading comment and separators
                targetLine = targetLine.TrimStart(';', ':', ' ', '\t');

                // --- direct known pattern detection ---
                // Example: ";Version: -2" → match "-2"
                var directMatch = Regex.Match(targetLine, @"Version[:\s]*(-\d+)", RegexOptions.IgnoreCase);
                if (directMatch.Success)
                {
                    var number = directMatch.Groups[1].Value.Substring(1); // skip dash
                    return "-." + number;  // -> "-.2"
                }

                // Step 4: fallback — split line on separators (. ; , |)
                var tokens = targetLine.Split(new[] { '.', ';', ',', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // try each token for a known pattern "-<number>"
                foreach (var token in tokens)
                {
                    var match = Regex.Match(token, @"-(\d+)");
                    if (match.Success)
                        return "-." + match.Groups[1].Value;
                }

                // Step 5: no version pattern found
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion
    }
}
