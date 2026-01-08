using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.PlannerBlocks.Position;
using SMSgroup.Aveva.Utilities.Writer.Interfaces;
using System.Text;

namespace PlantGateway.Application.Pipelines.Writer.Strategies.ProjectStructure
{
    public sealed class ProjectStructureTxtWriter : IWriterStrategy<ProjectStructureDTO>
    {
        // ==============================
        // ==  FIELDS  &  CONSTRUCTOR  ==
        // ==============================

        #region Fields and Constructor

        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigProvider _configProvider;

        private string StructureHeader = string.Empty;
        private string AttributesHeader = string.Empty;
        private string ImportHeader = string.Empty;

        public ProjectStructureTxtWriter(IServiceProvider serviceProvider, IConfigProvider configProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        #endregion

        // ==============================
        // ========  PUBLIC API  ========
        // ==============================

        #region Public API

        public void Write(PipelineContract<ProjectStructureDTO> contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (contract.Output == null)
                throw new InvalidOperationException("❌ Missing OutputTarget in PipelineContract.");
            if (!contract.Output.Paths.TryGetValue("File", out var fileDef))
                throw new InvalidOperationException("❌ No file path defined in OutputTarget for ProjectStructure.");

            var tree = contract.ConsolidatedTree
                       ?? throw new InvalidOperationException("❌ ConsolidatedTree not found — hierarchy not built before writing.");

            var dtoLookup = contract.ItemsLookup
                           ?? throw new InvalidOperationException("❌ ItemsLookup missing or empty in PipelineContract.");

            // Paths for all three files
            if (fileDef.Identifiers.TryGetValue("Structure", out var structureId))
            {
                WriteStructureHeader(contract);

                WriteStructure(structureId.Value, tree);
            }

            if (fileDef.Identifiers.TryGetValue("Attributes", out var attributesId))
            {
                WriteAttributeHeader(contract);

                WriteAttributes(attributesId.Value, tree, dtoLookup);
            }

            if (fileDef.Identifiers.TryGetValue("Import", out var importId))
            {
                WriteImportHeader(contract);

                WriteImport(importId.Value, tree, dtoLookup);
            }

            Console.WriteLine("✅ ProjectStructure TXT written successfully.");
        }

        #endregion

        // ==============================
        // =====  PRIVATE HELPERS  ======
        // ==============================

        #region Private Helpers

        #region Output Headers

        private void WriteStructureHeader(PipelineContract<ProjectStructureDTO> contract)
        {
            StructureHeader = "OWNER_AVEVA_TYPE|OWNER_AVEVA_TAG|AVEVA_TYPE|AVEVA_TAG";
        }

        private void WriteAttributeHeader(PipelineContract<ProjectStructureDTO> contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));

            //var outputCsysOption = contract.CsysOption;
            string headerPosWRT = "POSITION";
            string headerOriWRT = "ORIENTATION";
            string headerVersion = "VERSION";

            // Always follow the CLI option to output Pos and Ori, but leave the headers POSITION | ORIENTATION
            // This must stay like this from nw on, due to a fact of dynamic Aveva 'WRT' assignment.
            // Good to have is the commented output file header block with readable info about the output.

            //switch (outputCsysOption)
            //{
            //    case CsysOption.Absolute:
            //        headerPosWRT = "POSITION";
            //        headerOriWRT = "ORIENTATION";
            //        break;
            //    case CsysOption.Global:
            //        headerPosWRT = "POSITION WRT ?";
            //        headerOriWRT = "ORIENTATION WRT ?";
            //        break;
            //    case CsysOption.Relative:
            //        headerPosWRT = "POSITION WRT OWNER";
            //        headerOriWRT = "ORIENTATION WRT OWNER";
            //        break;
            //    case CsysOption.Transformed:
            //        headerPosWRT = "POSITION WRT OWNER";
            //        headerOriWRT = "ORIENTATION WRT OWNER";
            //        break;
            //    case CsysOption.WithOffset:
            //    default:
            //        headerPosWRT = "POSITION";
            //        headerOriWRT = "ORIENTATION";
            //        break;
            //}

            AttributesHeader = $"AVEVA_TAG|{headerPosWRT}|{headerOriWRT}|{headerVersion}";
        }
        private void WriteImportHeader(PipelineContract<ProjectStructureDTO> contract)
        {
            ImportHeader = "AVEVA_TAG|FILE";
        }

        #endregion

        #region Output Structure

        private void WriteStructure(string path, HierarchyTree tree)
        {
            var lines = new List<string> { StructureHeader };
            foreach (var root in tree.Roots)
                CollectStructureLines(root, null, lines);

            File.WriteAllLines(path, lines, Encoding.UTF8);
        }

        private void CollectStructureLines(HierarchyNode node, HierarchyNode? parent, List<string> lines)
        {
            bool isTopWorl = parent == null && node.AvevaType.Equals("WORL", StringComparison.OrdinalIgnoreCase);

            // Skip top WORL, but still traverse children
            if (!isTopWorl)
            {
                var ownerType = parent?.AvevaType ?? "*";
                var ownerTag = parent?.AvevaTag ?? "*";

                // Replace root WORL tag with "*"
                if (parent?.AvevaType.Equals("WORL", StringComparison.OrdinalIgnoreCase) == true)
                    ownerTag = "*";

                // --- Check SUB_SITE relation ---
                bool isNodeSubSite = node.AvevaType.Equals("SUB_SITE", StringComparison.OrdinalIgnoreCase);
                bool isOwnerSubSite = parent?.AvevaType.Equals("SUB_SITE", StringComparison.OrdinalIgnoreCase) == true;

                // Override the STRU with EQUI due to Aveva Import MSCAD limitation.
                bool isNodeSTRU = node.AvevaType.Equals("STRU", StringComparison.OrdinalIgnoreCase);
                if (isNodeSTRU)
                    node.AvevaType = "EQUI";

                // Apply prefix if node OR owner is SUB_SITE
                if (isNodeSubSite)
                    node.AvevaType = ":" + node.AvevaType;
                if (isOwnerSubSite)
                    ownerType = ":" + ownerType;

                lines.Add($"{ownerType}|{ownerTag}|{node.AvevaType}|{node.AvevaTag}");
            }

            // Recurse deeper
            foreach (var child in node.Children)
                CollectStructureLines(child, node, lines);
        }

        #endregion

        #region Output Attributes

        private void WriteAttributes(string path, HierarchyTree tree, IReadOnlyDictionary<Guid, ProjectStructureDTO> dtoLookup)
        {
            var lines = new List<string> { AttributesHeader };
            foreach (var node in FlattenTree(tree.Roots))
            {
                if (node.IsVirtual || node.Id == Guid.Empty)
                    continue;

                // Skip WORL – cannot have modifiable attributes
                if (node.AvevaType.Equals("WORL", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!dtoLookup.TryGetValue(node.Id, out var dto))
                    continue;

                var printAvevaTag = node.AvevaTag;
                var printPosition = GetBestPosition(dto);
                var printOrientation = GetBestOrientation(dto);
                var printVersion = dto.Version ?? string.Empty;

                if (string.IsNullOrWhiteSpace(printAvevaTag) &&
                    string.IsNullOrWhiteSpace(printPosition) &&
                    string.IsNullOrWhiteSpace(printOrientation) &&
                    string.IsNullOrWhiteSpace(printVersion))
                    continue;

                if (printPosition.Contains("SUB_SITE"))
                    printPosition = printPosition.Replace("SUB_SITE", ":SUB_SITE");

                if (printOrientation.Contains("SUB_SITE"))
                    printOrientation = printOrientation.Replace("SUB_SITE", ":SUB_SITE");

                lines.Add($"{printAvevaTag}|{printPosition}|{printOrientation}|{printVersion}");
            }

            File.WriteAllLines(path, lines, Encoding.UTF8);
        }

        #endregion

        #region Output Import
        private void WriteImport(string path, HierarchyTree tree, IReadOnlyDictionary<Guid, ProjectStructureDTO> dtoLookup)
        {
            var lines = new List<string> { ImportHeader };
            foreach (var node in FlattenTree(tree.Roots))
            {
                if (!node.AvevaType.Equals("EQUI", StringComparison.OrdinalIgnoreCase) &&
                    !node.AvevaType.Equals("STRU", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (node.IsVirtual || node.Id == Guid.Empty)
                    continue;

                if (!dtoLookup.TryGetValue(node.Id, out var dto))
                    continue;

                var geometry = dto.Geometry ?? string.Empty;
                if (string.IsNullOrWhiteSpace(geometry))
                    continue;

                lines.Add($"{node.AvevaTag}|{geometry}");
            }

            File.WriteAllLines(path, lines, Encoding.UTF8);
        }

        #endregion

        #region Output Common Helpers

        private static IEnumerable<HierarchyNode> FlattenTree(IEnumerable<HierarchyNode> roots)
        {
            foreach (var root in roots)
            {
                yield return root;
                foreach (var child in FlattenTree(root.Children))
                    yield return child;
            }
        }

        #endregion

        #region Print Helpers

        private static string GetBestPosition(ProjectStructureDTO dto)
        {
            // Try Absolute → Global → Relative
            var order = new[]
            {
                CsysOption.Absolute,
                CsysOption.Global,
                CsysOption.Relative
            };

            foreach (var option in order)
            {
                var posResult = dto.EngineResults
                    .OfType<PositionEngineResult>()
                    .FirstOrDefault();

                if (posResult == null)
                    return string.Empty;

                MatrixMetadata? matrix = option switch
                {
                    CsysOption.Absolute => dto.AbsoluteMatrix4x4,
                    CsysOption.Global => dto.GlobalMatrix4x4,
                    CsysOption.Relative => dto.Matrix4x4,
                    _ => null
                };

                if (matrix == null || !matrix.HasInput || !matrix.IsValid)
                    continue; // skip invalid or missing matrices

                // Pick corresponding orientation string
                var value = option switch
                {
                    CsysOption.Absolute => posResult.PositionAbsolute,
                    CsysOption.Global => posResult.PositionGlobal,
                    CsysOption.Relative => posResult.PositionRelative,
                    _ => string.Empty
                };

                if (!string.IsNullOrWhiteSpace(value) && !value.Contains("Skipped", StringComparison.OrdinalIgnoreCase))
                    return value.Trim();
            }

            // Nothing found
            return string.Empty;
        }

        private static string GetBestOrientation(ProjectStructureDTO dto)
        {
            // Try Absolute → Global → Relative
            var order = new[]
            {
                CsysOption.Absolute,
                CsysOption.Global,
                CsysOption.Relative
            };

            foreach (var option in order)
            {
                var oriResult = dto.EngineResults
                    .OfType<OrientationEngineResult>()
                    .FirstOrDefault();

                if (oriResult == null)
                    return string.Empty;

                MatrixMetadata? matrix = option switch
                {
                    CsysOption.Absolute => dto.AbsoluteMatrix4x4,
                    CsysOption.Global => dto.GlobalMatrix4x4,
                    CsysOption.Relative => dto.Matrix4x4,
                    _ => null
                };

                if (matrix == null || !matrix.HasInput || !matrix.IsValid)
                    continue;

                var value = option switch
                {
                    CsysOption.Absolute => oriResult.OrientationAbsolute,
                    CsysOption.Global => oriResult.OrientationGlobal,
                    CsysOption.Relative => oriResult.OrientationRelative,
                    _ => string.Empty
                };

                if (!string.IsNullOrWhiteSpace(value) && !value.Contains("Skipped", StringComparison.OrdinalIgnoreCase))
                    return value.Trim();
            }

            return string.Empty;
        }

        #endregion


        #endregion
    }
}
