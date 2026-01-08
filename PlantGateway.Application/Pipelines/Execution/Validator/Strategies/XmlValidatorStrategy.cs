using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Validator;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using PlantGateway.Application.Pipelines.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PlantGateway.Application.Pipelines.Execution.Validator.Interfaces;
using PlantGateway.Application.Pipelines.Execution.Validator.Rules.Attributes;

namespace PlantGateway.Application.Pipelines.Execution.Validator.Strategies
{
    /// <summary>
    /// Validator strategy for XML-based inputs (ProjectStructureDTO, TakeOverPointDTO).
    /// 
    /// Mirrors XmlParserStrategy:
    /// - PreProcess: load XML document into memory
    /// - Process: run document/element/attribute validators
    /// - PostProcess: cleanup
    /// </summary>
    public sealed class XmlValidatorStrategy : IValidatorStrategy
    {
        // ==========================================================
        // ==============  FIELDS AND CONSTRUCTOR  ==================
        // ==========================================================

        #region Fields and constructor

        private readonly IConfigProvider _configProvider;
        private readonly IReadOnlyList<IXDocumentValidator> _documentValidators;
        private readonly IReadOnlyList<IXElementValidator> _elementValidators;
        private readonly IReadOnlyList<IXAttributeValidator> _attributeValidators;

        private XDocument? _xDocument;

        public InputDataFormat Format => InputDataFormat.xml;

        public XmlValidatorStrategy(
            IConfigProvider configProvider,
            IEnumerable<IXDocumentValidator> documentValidators,
            IEnumerable<IXElementValidator> elementValidators,
            IEnumerable<IXAttributeValidator> attributeValidators)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            _documentValidators = (documentValidators ?? Enumerable.Empty<IXDocumentValidator>()).ToList();
            _elementValidators = (elementValidators ?? Enumerable.Empty<IXElementValidator>()).ToList();
            _attributeValidators = (attributeValidators ?? Enumerable.Empty<IXAttributeValidator>()).ToList();
        }

        #endregion

        // ==========================================================
        // ===============  PUBLIC ENTRY POINTS  ====================
        // ==========================================================

        #region Public APIs

        public ValidatorResult Analyze(IPipelineContract pipeline)
        {
            if (pipeline is PipelineContract<ProjectStructureDTO> ps)
                return Analyze(ps);

            if (pipeline is PipelineContract<TakeOverPointDTO> top)
                return Analyze(top);

            throw new NotSupportedException(
                "XmlValidatorStrategy only supports TakeOverPointDTO and ProjectStructureDTO at this time.");
        }

        public ValidatorResult Analyze(PipelineContract<ProjectStructureDTO> contract)
        {
            var result = new ValidatorResult();

            PreProcess(contract, result);

            if (_xDocument != null)
            {
                Process(contract, result);
            }
            else
            {
                result.Errors.Add("XML document could not be loaded into memory for validation.");
            }

            PostProcess(contract, result);
            return result;
        }

        public ValidatorResult Analyze(PipelineContract<TakeOverPointDTO> contract)
        {
            var result = new ValidatorResult();

            PreProcess(contract, result);

            if (_xDocument != null)
            {
                ProcessTakeOverPoint(contract, result);
            }
            else
            {
                result.Errors.Add("XML document could not be loaded into memory for validation.");
            }

            PostProcess(contract, result);
            return result;
        }

        #endregion

        // ==========================================================
        // ===============  PRIVATE PHASES  =========================
        // ==========================================================

        #region PreProcess

        private void PreProcess(PipelineContract<ProjectStructureDTO> contract, ValidatorResult validatorResult)
        {
            LoadFileToMemory(contract.Input.FilePath);
        }

        private void PreProcess(PipelineContract<TakeOverPointDTO> contract, ValidatorResult validatorResult)
        {
            // For now TOP XML validation is same pattern (if XML); we still load file.
            LoadFileToMemory(contract.Input.FilePath);
        }

        #endregion

        #region Process

        private void Process(PipelineContract<ProjectStructureDTO> contract, ValidatorResult result)
        {
            var doc = _xDocument;
            if (doc == null)
                return;

            // Registry for name uniqueness within this document
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenAvevaTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // --- Document-level validators (Header/Body/etc.) ---
            foreach (var validator in _documentValidators)
            {
                if (validator.CanHandle(doc))
                    validator.Validate(doc, result);
            }

            // --- Element- and attribute-level validators ---
            foreach (var element in doc.Descendants())
            {
                // Element validators (Assembly, Part, etc.)
                foreach (var ev in _elementValidators)
                {
                    if (ev.CanHandle(element))
                        ev.Validate(element, result);
                }

                // Attribute validators + custom attribute rules
                foreach (var attr in element.Attributes())
                {
                    // Geometry rule (uses contract)
                    if (string.Equals(attr.Name.LocalName, "geom", StringComparison.OrdinalIgnoreCase))
                    {
                        AttributeRule_Geometry.Validate(attr, result, contract);
                    }

                    // Name rule (uses shared name registry)
                    if (string.Equals(attr.Name.LocalName, "name", StringComparison.OrdinalIgnoreCase))
                    {
                        AttributeRule_Name.Validate(attr, result, seenNames);
                    }

                    // AvevaTag rule (uses token engine from config)
                    if (string.Equals(attr.Name.LocalName, "avevatag", StringComparison.OrdinalIgnoreCase))
                    {
                        AttributeRule_AvevaTag.Validate(attr, result, seenAvevaTags, contract);
                    }

                    // Generic attribute validators from DI
                    foreach (var av in _attributeValidators)
                    {
                        if (av.CanHandle(attr))
                            av.Validate(attr, result);
                    }
                }
            }
        }

        private void ProcessTakeOverPoint(PipelineContract<TakeOverPointDTO> contract, ValidatorResult result)
        {
            // For now no TOP-specific rules. We still might reuse document/element/attribute validators later.
            var doc = _xDocument;
            if (doc == null)
                return;

            foreach (var validator in _documentValidators)
            {
                if (validator.CanHandle(doc))
                    validator.Validate(doc, result);
            }

            foreach (var element in doc.Descendants())
            {
                foreach (var ev in _elementValidators)
                {
                    if (ev.CanHandle(element))
                        ev.Validate(element, result);
                }

                foreach (var attr in element.Attributes())
                {
                    foreach (var av in _attributeValidators)
                    {
                        if (av.CanHandle(attr))
                            av.Validate(attr, result);
                    }
                }
            }
        }

        #endregion

        #region PostProcess

        private void PostProcess(PipelineContract<ProjectStructureDTO> contract, ValidatorResult result)
        {
            DisposeDocument();
        }

        private void PostProcess(PipelineContract<TakeOverPointDTO> contract, ValidatorResult result)
        {
            DisposeDocument();
        }

        #endregion

        // ==========================================================
        // ===============  LOW-LEVEL UTILITIES  ====================
        // ==========================================================

        #region Helpers
        private void LoadFileToMemory(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), "XML file path cannot be null or empty.");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("XML file not found.", filePath);

            _xDocument = XDocument.Load(
                filePath,
                LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo
            );
        }

        private void DisposeDocument()
        {
            _xDocument = null;
        }

        #endregion
    }
}
