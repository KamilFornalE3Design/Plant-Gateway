using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using PlantGateway.Application.Pipelines.Parser.Analyzers.Attributes;
using PlantGateway.Application.Pipelines.Parser.Analyzers.Document.Xml;
using PlantGateway.Application.Pipelines.Parser.Interfaces;
using System.Reflection.Metadata;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Parser.Strategies
{
    public sealed class XmlParserStrategy : IParserStrategy
    {
        // ==========================================================
        // ==============  FIELDS AND CONSTRUCTOR  ==================
        // ==========================================================

        #region Fields and Constructor

        private readonly IConfigProvider _configProvider;
        private readonly IReadOnlyList<IXDocumentAnalyzer> _documentAnalyzers;
        private readonly IReadOnlyList<IXElementAnalyzer> _elementAnalyzers;
        private readonly IReadOnlyList<IXAttributeAnalyzer> _attributeAnalyzers;

        private XDocument? xDocument;
        public InputDataFormat Format => InputDataFormat.xml;

        public XmlParserStrategy(IConfigProvider configProvider, IEnumerable<IXDocumentAnalyzer> documentAnalyzers, IEnumerable<IXElementAnalyzer> elementAnalyzers, IEnumerable<IXAttributeAnalyzer> attributeAnalyzers)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            _documentAnalyzers = (documentAnalyzers ?? Enumerable.Empty<IXDocumentAnalyzer>()).ToList();
            _elementAnalyzers = (elementAnalyzers ?? Enumerable.Empty<IXElementAnalyzer>()).ToList();
            _attributeAnalyzers = (attributeAnalyzers ?? Enumerable.Empty<IXAttributeAnalyzer>()).ToList();
        }

        #endregion

        // ==========================================================
        // ===============  PUBLIC ENTRY POINTS  ====================
        // ==========================================================


        #region Public APIs

        public ParserResult Analyze(IPipelineContract pipeline)
        {
            if (pipeline is PipelineContract<ProjectStructureDTO> ps)
                return Analyze(ps);

            if (pipeline is PipelineContract<TakeOverPointDTO> top)
                return Analyze(top);

            throw new NotSupportedException(
                "XmlParserStrategy only supports TakeOverPointDTO and ProjectStructureDTO at this time.");
        }

        public ParserResult Analyze(PipelineContract<ProjectStructureDTO> typedPipeline)
        {
            var result = new ParserResult();

            PreProcess(typedPipeline, result);

            if (xDocument != null)
            {
                Process(typedPipeline, result);
            }
            else
            {
                result.Error.Add("XML document could not be loaded into memory.");
            }

            PostProcess(typedPipeline, result);

            return result;
        }

        public ParserResult Analyze(PipelineContract<TakeOverPointDTO> typedPipeline)
        {
            var result = new ParserResult();

            PreProcess(typedPipeline, result);

            // TODO: implement TakeOverPoint XML document analyzers if needed

            PostProcess(typedPipeline, result);

            return result;
        }

        #endregion

        // ==========================================================
        // ===============  PRIVATE PHASES  =========================
        // ==========================================================

        #region PreProcess

        private void PreProcess(PipelineContract<ProjectStructureDTO> typedPipeline, ParserResult parserResult)
        {
            // work with XDocument inMemory
            LoadFileToMemory(typedPipeline.Input.FilePath, parserResult);
        }

        private void PreProcess(PipelineContract<TakeOverPointDTO> typedPipeline, ParserResult parserResult)
        {
            // No implementation.
        }

        #endregion

        #region Process

        private void Process(PipelineContract<ProjectStructureDTO> typedPipeline, ParserResult parserResult)
        {
            if (xDocument == null)
            {
                parserResult.Warnings.Add("XmlParserStrategy.Process: XDocument is null (file could not be loaded).");
                return;
            }

            var doc = xDocument;

            // ───────── 1) Document-level analyzers (Header/Body/Footer etc.) ─────────
            foreach (var docAnalyzer in _documentAnalyzers)
            {
                if (docAnalyzer.CanHandle(doc))
                {
                    docAnalyzer.Analyze(doc, parserResult);
                }
            }

            // ───────── 2) Element-level analyzers (AssemblyNodeAnalyzer, PartNodeAnalyzer, …) ─────────
            foreach (var element in doc.Descendants())
            {
                foreach (var elementAnalyzer in _elementAnalyzers)
                {
                    if (elementAnalyzer.CanHandle(element))
                    {
                        elementAnalyzer.Analyze(element, parserResult);
                    }
                }

                // ───────── 3) Attribute-level analyzers (if/when you add them) ─────────
                foreach (var attribute in element.Attributes())
                {
                    foreach (var attributeAnalyzer in _attributeAnalyzers)
                    {
                        if (attributeAnalyzer.CanHandle(attribute))
                        {
                            attributeAnalyzer.Analyze(attribute, parserResult);
                        }
                    }
                }
            }
        }

        #endregion

        #region PostProcess

        private void PostProcess(PipelineContract<ProjectStructureDTO> typedPipeline, ParserResult result)
        {
            DisposeProperties();
        }

        private void PostProcess(PipelineContract<TakeOverPointDTO> typedPipeline, ParserResult result)
        {
            DisposeProperties();
        }

        #endregion

        // ==========================================================
        // ===============  LOW-LEVEL UTILITIES  ====================
        // ==========================================================

        #region Load File

        private void LoadFileToMemory(string filePath, ParserResult parserResult)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), "XML file path cannot be null or empty.");

            if (!File.Exists(filePath))
            {
                parserResult.Error.Add($"XML file not found: '{filePath}'.");
                xDocument = null;
                return;
            }

            try
            {
                var xmlContent = File.ReadAllText(filePath);
                xDocument = XDocument.Parse(xmlContent, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            }
            catch (Exception ex)
            {
                parserResult.Error.Add($"Failed to parse XML file '{filePath}': {ex.Message}");
                xDocument = null;
            }
        }

        #endregion

        #region Properties Dispose

        private void DisposeProperties()
        {
            xDocument = null;
        }

        #endregion

    }
}
