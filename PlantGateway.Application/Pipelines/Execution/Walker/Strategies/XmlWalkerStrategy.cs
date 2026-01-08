using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Walker;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using PlantGateway.Application.Pipelines.Helpers;
using PlantGateway.Application.Pipelines.Walker.Interfaces;
using System.Xml;
using System.Xml.Linq;
using PlantGateway.Application.Pipelines.Execution.Walker.Interfaces;

namespace PlantGateway.Application.Pipelines.Execution.Walker.Strategies
{
    /// <summary>
    /// XML walker for ProjectStructureDTO.
    /// Builds a GUID-indexed dictionary of { XElement node, XElement? matrixNode }
    /// and stores it in the PipelineContract.Metadata for use by processors.
    /// </summary>
    public sealed class XmlWalkerStrategy : IWalkerStrategy<ProjectStructureDTO>
    {
        private readonly IConfigProvider _configProvider;
        private List<PGNode<XElement>> _pgNodeList = new List<PGNode<XElement>>();

        public InputDataFormat Format => InputDataFormat.xml;

        public XmlWalkerStrategy(IConfigProvider configProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public WalkerResult<ProjectStructureDTO> Walk(PipelineContract<ProjectStructureDTO> pipelineContract)
        {
            if (pipelineContract == null)
                throw new ArgumentNullException(nameof(pipelineContract));

            var filePath = pipelineContract.Input.FilePath;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("❌ XML file not found.", filePath);

            var xDoc = LoadFile(filePath);
            if (xDoc?.Root == null)
            {
                Console.WriteLine("⚠️ XML file has no root element or failed to load properly.");
                return new WalkerResult<ProjectStructureDTO>
                {
                    FilePath = filePath,
                    IsSuccess = false,
                    Warnings = { "XML has no root element or failed to load properly." }
                };
            }

            var result = new WalkerResult<ProjectStructureDTO>
            {
                FilePath = filePath,
                ParserHints = pipelineContract.ParserResult?.ParserHints ?? new Dictionary<string, object>(),
                IsSuccess = false
            };

            try
            {
                // 1️ Load all Assemblies and Parts into memory as PGNodes
                var pgNodes = PGNodeBuilder(xDoc.Root, result);

                // 2️ API placeholder – hook ParserResult / PlannerResult before DTO creation
                // e.g., Filter or augment pgNodes based on pipelineContract.ParserResult
                // TODO: pipelineContract.PlannerResult?.ApplyTo(pgNodes);

                // 3️ Convert PGNodes → ProjectStructureDTO
                var entries = new List<ProjectStructureDTO>();
                foreach (var node in pgNodes)
                {
                    var dto = new ProjectStructureDTO
                    {
                        Id = node.Identifier,
                        Name = XElementAttributeHelper.GetName(node.Node),
                        AvevaTag = XElementAttributeHelper.GetAvevaTag(node.Node),
                        DescriptionDE = XElementAttributeHelper.GetDescrDE(node.Node),
                        DescriptionEN = XElementAttributeHelper.GetDescrEN(node.Node),
                        ColorRGB = XElementAttributeHelper.GetColorRGB(node.Node),
                        ColorTranslucency = XElementAttributeHelper.GetColorTransp(node.Node),
                        Geometry = XElementAttributeHelper.GetGeom(node.Node),
                        Version = XElementAttributeHelper.GetWindchillVersion(node.Node),
                        OtherAttributes = XElementAttributeHelper.GetOtherAttributes(node.Node),

                        FileFullPath = XElementAttributeHelper.ResolveStpFile(node.Node),
                        FolderPath = Path.GetDirectoryName(XElementAttributeHelper.ResolveStpFile(node.Node)) ?? string.Empty,

                        Matrix4x4 = MatrixHelper.GetMatrixMetadata(node.Matrix),
                        GlobalMatrix4x4 = MatrixHelper.GetMatrixMetadata(node.GlobalMatrix),
                        AbsoluteMatrix4x4 = MatrixHelper.GetMatrixMetadata(node.AbsoluteMatrix),
                        TransformedMatrix4x4 = MatrixHelper.Identity(),
                        IsComponent = XElementAttributeHelper.DeductIsComponent(node.Node)
                    };

                    entries.Add(dto);
                }

                // 4️ Update pipeline contract + result
                pipelineContract.Metadata = pgNodes;
                pipelineContract.Items = entries;
                pipelineContract.ItemsLookup = entries.ToDictionary(x => x.Id, x => x);
                result.ParserHints["NodeCount"] = pgNodes.Count;
                result.Dtos.AddRange(entries);
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Warnings.Add($"❌ Walker failed: {ex.Message}");
            }

            return result;
        }

        // --------------------------------------------------------------------
        // 🔹 Dictionary Builder
        // --------------------------------------------------------------------

        public static List<PGNode<XElement>> PGNodeBuilder(XElement root, WalkerResult<ProjectStructureDTO> walkerResult)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(walkerResult);

            return root
                .DescendantsAndSelf()
                .Select(x => new { Element = x, Key = TryGetNodeKey(x.Name.LocalName, walkerResult) })
                .Where(x => x.Key is not PGNodeKey.Unset and (PGNodeKey.Assembly or PGNodeKey.Part))
                .Select(x => new PGNode<XElement>
                {
                    Type = x.Key,
                    Node = x.Element,
                    Matrix = TryGetElementByKey(x.Element, PGNodeKey.Matrix),
                    GlobalMatrix = TryGetElementByKey(x.Element, PGNodeKey.GlobalMatrix),
                    Identifier = Guid.NewGuid()
                })
                .ToList();
        }

        private static PGNodeKey TryGetNodeKey(string localName, WalkerResult<ProjectStructureDTO> walkerResult)
        {
            if (Enum.TryParse(localName, true, out PGNodeKey key))
                return key;

            walkerResult.Warnings.Add($"⚠️ Unknown node '{localName}' ignored.");
            return PGNodeKey.Unset;
        }

        private static XElement? TryGetElementByKey(XElement? parent, PGNodeKey key)
        {
            if (parent is null || key is PGNodeKey.Unset)
                return null;

            var name = key.ToString();
            return parent
                .Elements()
                .FirstOrDefault(e =>
                    !string.IsNullOrEmpty(e.Name.LocalName) &&
                    string.Equals(e.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));
        }

        // TODO: convert fnc name to 'InMemory' to match the flow and parser, and move to private field.
        private static XDocument LoadFile(string filePath)
        {
            using var reader = XmlReader.Create(filePath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
            return XDocument.Load(reader, LoadOptions.SetLineInfo | LoadOptions.SetBaseUri);
        }
    }
}
