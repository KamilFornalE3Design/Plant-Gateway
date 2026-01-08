using Microsoft.Extensions.DependencyInjection;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using PlantGateway.Application.Pipelines.Writer.Interfaces;
using System.Xml;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Writer.Strategies.ProjectStructure
{
    /// <summary>
    /// Writes the consolidated AVEVA hierarchy structure into XML format.
    /// Only includes element type nodes (WORL, SITE, SUB_SITE, ZONE, EQUI, STRU)
    /// and minimal attributes: AvevaTag + IsVirtual (diagnostic phase).
    /// </summary>
    public sealed class ProjectStructureXmlWriter : IWriterStrategy<ProjectStructureDTO>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigProvider _configProvider;

        private readonly IHierarchyTreeMapService _hierarchyMapService;

        public ProjectStructureXmlWriter(IServiceProvider serviceProvider, IConfigProvider configProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

            _hierarchyMapService = _serviceProvider.GetRequiredService<IHierarchyTreeMapService>();
        }

        public void Write(PipelineContract<ProjectStructureDTO> contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));

            // === 1️⃣ Validate Output Target ===
            if (contract.Output == null)
                throw new InvalidOperationException("❌ Missing OutputTarget in PipelineContract.");

            if (!contract.Output.Paths.TryGetValue("File", out var fileDef))
                throw new InvalidOperationException("❌ No file path defined in OutputTarget for ProjectStructure.");

            if (!fileDef.Identifiers.TryGetValue("Hierarchy", out var hierarchyId))
                throw new InvalidOperationException("❌ No Hierarchy output identifier defined in OutputTarget.");

            var emptyDtos = contract.Items
                .Where(dto => dto.EngineResults
                    .OfType<RoleEngineResult>()
                    .Any(r => string.Equals(r.AvevaType, string.Empty, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var outputPath = hierarchyId.Value;

            // === 2️⃣ Validate hierarchy ===
            var tree = contract.ConsolidatedTree
                       ?? throw new InvalidOperationException("❌ ConsolidatedTree not found — hierarchy not built before writing.");

            // === 3️⃣ Use ItemsLookup for fast lookup ===
            var dtoLookup = contract.ItemsLookup;
            if (dtoLookup == null || dtoLookup.Count == 0)
                throw new InvalidOperationException("❌ ItemsLookup missing or empty in PipelineContract.");

            // === 4️⃣ Proceed to write ===
            WriteHierarchyXml(outputPath, tree, dtoLookup);
            Console.WriteLine($"✅ ProjectStructure Hierarchy XML written to [{Path.GetFileName(outputPath)}]");
        }

        /// <summary>
        /// Writes the full hierarchy XML to file using UTF-8 indentation.
        /// </summary>
        private void WriteHierarchyXml(string outputPath, HierarchyTree tree, IReadOnlyDictionary<Guid, ProjectStructureDTO> dtoLookup)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = System.Text.Encoding.UTF8,
                OmitXmlDeclaration = false
            };

            using var writer = XmlWriter.Create(outputPath, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("ProjectStructure");

            foreach (var root in tree.Roots.OrderBy(r => r.AvevaType))
                WriteNodeRecursive(writer, root, dtoLookup);

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        /// <summary>
        /// Recursively writes hierarchy nodes, resolving Position and Orientation
        /// from <see cref="ProjectStructureDTO.EngineResults"/>.
        /// </summary>
        private void WriteNodeRecursive(XmlWriter writer, HierarchyNode node, IReadOnlyDictionary<Guid, ProjectStructureDTO> dtoLookup)
        {
            writer.WriteStartElement(node.AvevaType);
            writer.WriteAttributeString("AvevaTag", node.AvevaTag);
            writer.WriteAttributeString("IsVirtual", node.IsVirtual.ToString());
            writer.WriteAttributeString("OriginId", node.Id.ToString());

            if (!node.IsVirtual && dtoLookup.TryGetValue(node.Id, out var dto))
                WriteNodeAttributes(writer, dto);

            foreach (var child in node.Children.OrderBy(c => c.AvevaType))
                WriteNodeRecursive(writer, child, dtoLookup);

            writer.WriteEndElement();
        }

        /// <summary>
        /// Adds position/orientation attributes from engine results if available.
        /// </summary>
        private static void WriteNodeAttributes(XmlWriter writer, ProjectStructureDTO dto)
        {
            var posResult = dto.EngineResults.OfType<PositionEngineResult>().FirstOrDefault();
            var oriResult = dto.EngineResults.OfType<OrientationEngineResult>().FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(posResult?.Position))
                writer.WriteAttributeString("Position", posResult.Position);

            if (!string.IsNullOrWhiteSpace(oriResult?.Orientation))
                writer.WriteAttributeString("Orientation", oriResult.Orientation);
        }
        /// <summary>
        /// Writes link metadata back to XML output for non-virtual nodes.
        /// Uses the walker-generated Metadata dictionary to map DTOs ↔ XElement.
        /// </summary>
        private static void WriteSourceLink(XmlWriter writer, Guid id, Dictionary<Guid, Dictionary<XElement, XElement>> metadata)
        {
            if (metadata == null || !metadata.TryGetValue(id, out var map))
                return;

            var sourceNode = map.Keys.FirstOrDefault();
            if (sourceNode == null)
                return;

            // Determine line number if available
            var lineInfo = (IXmlLineInfo)sourceNode;
            var line = lineInfo.HasLineInfo() ? lineInfo.LineNumber : -1;

            // Add attributes for traceability or UI click-linking
            writer.WriteAttributeString("SourceFile", Path.GetFileName(sourceNode.BaseUri ?? string.Empty));
            writer.WriteAttributeString("SourceLine", line.ToString());
        }

    }
}
