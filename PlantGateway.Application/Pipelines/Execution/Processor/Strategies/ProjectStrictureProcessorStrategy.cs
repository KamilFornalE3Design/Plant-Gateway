using Microsoft.Extensions.DependencyInjection;
using PlantGateway.Application.Pipelines.Execution.Processor.Interfaces;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Data.IdentityCache;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using SMSgroup.Aveva.Utilities.Engines;
using SMSgroup.Aveva.Utilities.Engines.Disposition;
using SMSgroup.Aveva.Utilities.Helpers;
using SMSgroup.Aveva.Utilities.Processor.Interfaces;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Execution.Processor.Strategies
{
    /// <summary>
    /// Processor strategy for <see cref="ProjectStructureDTO"/>.
    /// Runs tokenization, enrichment, and transformation in defined phases.
    /// </summary>
    public sealed class ProjectStructureProcessorStrategy : IProcessorStrategy<ProjectStructureDTO>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigProvider _configProvider;

        public ProjectStructureProcessorStrategy(IServiceProvider serviceProvider, IConfigProvider configProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public PipelineContract<ProjectStructureDTO> Process(PipelineContract<ProjectStructureDTO> contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));

            // === Resolve services ===
            var disciplineMapService = _serviceProvider.GetRequiredService<IDisciplineMapService>();
            var entityMapService = _serviceProvider.GetRequiredService<IEntityMapService>();
            var roleMapService = _serviceProvider.GetRequiredService<IRoleMapService>();
            var tokenregexMapService = _serviceProvider.GetRequiredService<ITokenRegexMapService>();
            var allowedTreeMapService = _serviceProvider.GetRequiredService<IAllowedTreeMapService>();
            var disciplineHierarchyTokenMapService = _serviceProvider.GetRequiredService<IDisciplineHierarchyTokenMapService>();
            var codificationMapService = _serviceProvider.GetRequiredService<ICodificationMapService>();
            var identityCacheService = _serviceProvider.GetRequiredService<TakeOverPointCacheService>();

            // === Instantiate all engines ===
            var tokenEngine = new TokenEngine(disciplineHierarchyTokenMapService, tokenregexMapService, disciplineMapService, entityMapService, codificationMapService);
            var namingEngine = new NamingEngine(disciplineHierarchyTokenMapService, tokenregexMapService);
            var disciplineEngine = new DisciplineEngine(disciplineMapService);
            var hierarchyEngine = new HierarchyEngine(disciplineHierarchyTokenMapService, allowedTreeMapService);
            var tagEngine = new TagEngine();
            var transformationEngine = new TransformationEngine();
            var positionEngine = new PositionEngine(contract.CsysOption, contract.CsysWRT, contract.CsysReferenceOffset);
            var orientationEngine = new OrientationEngine(contract.CsysOption, contract.CsysWRT, contract.CsysReferenceOffset);
            var entityEngine = new EntityEngine(entityMapService);
            var roleEngine = new RoleEngine(roleMapService);
            var suffixEngine = new SuffixEngine(disciplineHierarchyTokenMapService, identityCacheService);
            var dispositionEngine = new DispositionEngine();

            // === PHASE 1: PREPROCESS (Tokenization) ===
            PreProcess(contract, tokenEngine);

            // === PHASE 2: PROCESS (Naming, Discipline, Hierarchy, Tag) ===
            ProcessMain(contract, namingEngine, disciplineEngine, hierarchyEngine, entityEngine, tagEngine, roleEngine, suffixEngine, dispositionEngine);

            // === PHASE 3: POSTPROCESS (Transform, Position, Orientation) ===
            PostProcess(contract, transformationEngine, positionEngine, orientationEngine);

            RunEngineCoverageDiagnostics(contract);

            return contract;
        }

        // ===============================================================
        // 🔹 PHASE 1 — PREPROCESS
        // ===============================================================
        /// <summary>
        /// Tokenizes DTOs to prepare base semantic information.
        /// </summary>
        private void PreProcess(PipelineContract<ProjectStructureDTO> contract, TokenEngine tokenEngine)
        {
            contract.HierarchyGroups = BuildHierarchyGroupsLinq(contract.Metadata).ToList();

            var pgNodes = contract.Metadata?.ToList() ?? new List<PGNode<XElement>>();

            foreach (var dto in contract.Items)
            {
                // Assign top-level assembly Guid
                dto.TopLevelAssemblyId = ResolveTopLevelAssemblyId(dto.Id, contract.HierarchyGroups, pgNodes);

                // Run tokenization
                var tokenResult = tokenEngine.Execute(dto);
                AddIfNotNull(dto, contract, tokenResult);
            }
        }

        // ===============================================================
        // 🔹 PHASE 2 — PROCESS
        // ===============================================================
        /// <summary>
        /// Enriches DTOs with semantic data: naming, discipline, hierarchy, tag.
        /// </summary>
        private void ProcessMain(PipelineContract<ProjectStructureDTO> contract, NamingEngine namingEngine, DisciplineEngine disciplineEngine, HierarchyEngine hierarchyEngine, EntityEngine entityEngine, TagEngine tagEngine, RoleEngine roleEngine, SuffixEngine suffixEngine, DispositionEngine dispositionEngine)
        {
            try
            {
                foreach (var group in contract.HierarchyGroups.OrderBy(g => g.Key))
                {
                    foreach (var dtoId in group)
                    {
                        var dto = contract.Items.FirstOrDefault(x => x.Id == dtoId);
                        if (dto == null) continue;

                        var parentDto = GetParentDto(dto, contract);

                        // --- Entity inheritance ---
                        var inheritedEntity = parentDto != null
                            ? (parentDto.Id, parentDto.EngineResults.OfType<EntityEngineResult>().FirstOrDefault()?.Entity ?? "SDE")
                            : (Guid.Empty, "SDE");

                        var entityResult = entityEngine.Process(dto, inheritedEntity);
                        AddIfNotNull(dto, contract, entityResult);

                        // --- Discipline inheritance ---
                        var inheritedDiscipline = parentDto != null
                            ? (parentDto.Id, parentDto.EngineResults.OfType<DisciplineEngineResult>().FirstOrDefault()?.Discipline ?? "ME")
                            : (Guid.Empty, "ME");

                        var disciplineResult = disciplineEngine.Process(dto, inheritedDiscipline);
                        AddIfNotNull(dto, contract, disciplineResult);

                        // --- Remaining processing ---
                        AddIfNotNull(dto, contract, roleEngine.Process(dto));
                        AddIfNotNull(dto, contract, suffixEngine.Process(dto));
                        AddIfNotNull(dto, contract, namingEngine.Execute(dto));
                        AddIfNotNull(dto, contract, tagEngine.Process(dto));
                        AddIfNotNull(dto, contract, dispositionEngine.Process(dto));
                        AddIfNotNull(dto, contract, hierarchyEngine.Process(dto, contract));
                    }
                }

                // Consolidate hierarchy once at the end
                ConsolidateHierarchy(contract);
            }
            catch (Exception ex)
            {
                contract.ProcessorResult.AddError($"Exception during processing: {ex.Message}");

                Console.WriteLine($"⚠️ [ProcessMain] Exception during processing: {ex.Message}");
            }
        }

        // ===============================================================
        // 🔹 PHASE 3 — POSTPROCESS
        // ===============================================================
        /// <summary>
        /// Applies transformations, computes global matrices, and extracts position/orientation.
        /// </summary>
        private void PostProcess(
            PipelineContract<ProjectStructureDTO> contract,
            TransformationEngine transformationEngine,
            PositionEngine positionEngine,
            OrientationEngine orientationEngine)
        {
            Console.WriteLine("📐 [PostProcess] Applying transformations and computing positions/orientations...");

            if (contract.Metadata is not List<PGNode<XElement>> pgNodes || pgNodes.Count == 0)
            {
                Console.WriteLine("⚠️ Metadata is not a valid PGNode list. Transformation skipped.");
                return;
            }

            var dtos = contract.Items?.ToList() ?? new List<ProjectStructureDTO>();

            // 1️⃣ Select top-level Assembly nodes (no Assembly parent)
            var topLevelNodes = pgNodes
                .Where(n =>
                    n.Type == PGNodeKey.Assembly &&
                    (n.Node.Parent == null ||
                     !string.Equals(n.Node.Parent.Name.LocalName, "Assembly", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // 2️⃣ Apply transformations recursively
            foreach (var node in topLevelNodes)
                ApplyTransformationRecursive(node, MatrixHelper.Identity(), pgNodes, dtos, transformationEngine);

            // 3️⃣ Compute positions & orientations
            foreach (var dto in dtos)
            {
                AddIfNotNull(dto, contract, positionEngine.Process(dto, contract));
                AddIfNotNull(dto, contract, orientationEngine.Process(dto, contract));
            }

            Console.WriteLine("✔ [PostProcess] Transformations, positions, and orientations completed.");

        }

        // ===============================================================
        // 🔹 RECURSIVE TRANSFORMATION
        // ===============================================================
        private static void ApplyTransformationRecursive(
            PGNode<XElement> currentNode,
            double[,] parentMatrix,
            List<PGNode<XElement>> allNodes,
            List<ProjectStructureDTO> dtos,
            TransformationEngine transformationEngine)
        {
            // 1️⃣ Compute local + transformed matrix
            var localMatrix = MatrixHelper.GetMatrixMetadata(currentNode.Matrix);
            var transformedMatrix = transformationEngine.Transform(localMatrix, parentMatrix);

            // 2️⃣ Find matching DTO
            var dto = dtos.FirstOrDefault(d =>
                string.Equals(d.AvevaTag,
                    XElementAttributeHelper.GetAvevaTag(currentNode.Node),
                    StringComparison.OrdinalIgnoreCase));

            if (dto != null)
            {
                dto.Matrix4x4 = localMatrix;
                dto.TransformedMatrix4x4 = transformedMatrix;
                dto.GlobalMatrix4x4 = MatrixHelper.GetMatrixMetadata(currentNode.GlobalMatrix);
                dto.AbsoluteMatrix4x4 = MatrixHelper.GetMatrixMetadata(currentNode.AbsoluteMatrix);
            }

            // 3️⃣ Recurse into child Assemblies or Parts
            var children = allNodes
                .Where(n =>
                    n.Node.Parent == currentNode.Node &&
                    (n.Type == PGNodeKey.Assembly || n.Type == PGNodeKey.Part))
                .ToList();

            foreach (var child in children)
                ApplyTransformationRecursive(child, transformedMatrix, allNodes, dtos, transformationEngine);
        }


        // ===============================================================
        // 🔹 UTILS
        // ===============================================================
        private static void AddIfNotNull(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract, IEngineResult? result)
        {
            if (result == null) return;
            dto.EngineResults.Add(result);
        }
        /// <summary>
        /// Groups project structure nodes (PGNode) by their hierarchy depth,
        /// calculated from their XML parent chain.
        /// </summary>
        private IEnumerable<IGrouping<int, Guid>> BuildHierarchyGroupsLinq(IEnumerable<PGNode<XElement>> nodes)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes), "PGNode collection cannot be null.");

            // Filter out invalid or empty nodes to avoid null reference exceptions
            var validNodes = nodes
                .Where(n => n != null && n.Node != null)
                .ToList();

            var grouped = validNodes
                .Select(node =>
                {
                    int depth = 0;
                    var element = node.Node;

                    // Walk up the XML tree until Root (like before)
                    var parent = element.Parent;
                    while (parent != null && !parent.Name.LocalName.Equals("Root", StringComparison.OrdinalIgnoreCase))
                    {
                        depth++;
                        parent = parent.Parent;
                    }

                    return new { node.Identifier, Depth = depth };
                })
                .GroupBy(x => x.Depth, x => x.Identifier)
                .OrderBy(g => g.Key);

            return grouped;
        }
        /// <summary>
        /// Resolves the top-level Assembly identifier for the given node.
        /// Returns self if the node is already top-level (self-relation is legal).
        /// </summary>
        private Guid ResolveTopLevelAssemblyId(Guid nodeId, IEnumerable<IGrouping<int, Guid>> hierarchyGroups, IEnumerable<PGNode<XElement>> nodes)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes), "PGNode collection cannot be null.");

            var currentNode = nodes.FirstOrDefault(n => n.Identifier == nodeId);
            if (currentNode == null || currentNode.Node == null)
                return Guid.Empty;

            // If this node itself is Assembly and has no parent Assembly → it's top-level
            var parentElement = currentNode.Node.Parent;
            var parentNode = nodes.FirstOrDefault(n => n.Node == parentElement);

            if (currentNode.Type == PGNodeKey.Assembly && (parentNode == null || parentNode.Type != PGNodeKey.Assembly))
            {
                // self-relation accepted; Position/OrientationEngine will handle owner redirect
                return currentNode.Identifier;
            }

            // Otherwise climb upward until we find an Assembly whose parent is not an Assembly
            var ancestor = parentNode;
            while (ancestor != null)
            {
                var ancestorParent = nodes.FirstOrDefault(n => n.Node == ancestor.Node.Parent);

                if (ancestor.Type == PGNodeKey.Assembly &&
                    (ancestorParent == null || ancestorParent.Type != PGNodeKey.Assembly))
                {
                    return ancestor.Identifier; // found top-level Assembly
                }

                ancestor = ancestorParent;
            }

            // fallback – no assembly found
            return Guid.Empty;
        }

        private void RunEngineCoverageDiagnostics(PipelineContract<ProjectStructureDTO> contract)
        {
            if (contract?.Items == null || contract.Items.Count == 0)
            {
                Console.WriteLine("⚠️ No DTOs available for diagnostics.");
                return;
            }

            var stats = contract.Items
                .Select(dto => new
                {
                    Name = dto.Name ?? dto.Id.ToString(),
                    dto.Id,
                    HasToken = dto.EngineResults.OfType<TokenEngineResult>().Any(),
                    HasRole = dto.EngineResults.OfType<RoleEngineResult>().Any(),
                    HasDiscipline = dto.EngineResults.OfType<DisciplineEngineResult>().Any(),
                    HasEntity = dto.EngineResults.OfType<EntityEngineResult>().Any(),
                    HasSuffix = dto.EngineResults.OfType<SuffixEngineResult>().Any()
                })
                .ToList();

            int total = stats.Count;
            int missingToken = stats.Count(x => !x.HasToken);
            int missingRole = stats.Count(x => !x.HasRole);
            int missingDisc = stats.Count(x => !x.HasDiscipline);
            int missingEntity = stats.Count(x => !x.HasEntity);
            int missingSuffix = stats.Count(x => !x.HasSuffix);

            Console.WriteLine("=== ENGINE RESULT COVERAGE SUMMARY ===");
            Console.WriteLine($"Total DTOs: {total}");
            Console.WriteLine($"Missing TokenEngineResult: {missingToken}");
            Console.WriteLine($"Missing RoleEngineResult: {missingRole}");
            Console.WriteLine($"Missing DisciplineEngineResult: {missingDisc}");
            Console.WriteLine($"Missing EntityEngineResult: {missingEntity}");
            Console.WriteLine($"Missing SuffixEngineResult: {missingSuffix}");
            Console.WriteLine();

            // === Optional XML output ===
            try
            {
                if (!string.IsNullOrWhiteSpace(contract.Input.FilePath))
                {
                    var dir = Path.GetDirectoryName(contract.Input.FilePath)!;
                    var file = Path.Combine(dir, Path.GetFileNameWithoutExtension(contract.Input.FilePath) + ".diag.xml");

                    var doc = new XDocument(
                        new XDeclaration("1.0", "utf-8", "yes"),
                        new XElement("Diagnostics",
                            new XAttribute("Total", total),
                            new XAttribute("MissingToken", missingToken),
                            new XAttribute("MissingRole", missingRole),
                            new XAttribute("MissingDiscipline", missingDisc),
                            new XAttribute("MissingEntity", missingEntity),
                            new XAttribute("MissingSuffix", missingSuffix),
                            new XElement("MissingItems",
                                stats
                                    .Where(x => !x.HasToken || !x.HasRole || !x.HasDiscipline || !x.HasEntity || !x.HasSuffix)
                                    .Select(x => new XElement("Item",
                                        new XAttribute("Id", x.Id),
                                        new XAttribute("Name", x.Name),
                                        new XAttribute("Token", x.HasToken),
                                        new XAttribute("Role", x.HasRole),
                                        new XAttribute("Discipline", x.HasDiscipline),
                                        new XAttribute("Entity", x.HasEntity),
                                        new XAttribute("Suffix", x.HasSuffix)
                                    ))
                            )
                        )
                    );

                    doc.Save(file);
                    Console.WriteLine($"📊 Diagnostics written to: {file}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to write diagnostic XML: {ex.Message}");
            }
        }

        private ProjectStructureDTO? GetParentDto(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract)
        {
            // 1️⃣ Ensure Metadata is a valid PGNode list
            if (contract.Metadata is not List<PGNode<XElement>> pgNodes || pgNodes.Count == 0)
                return null;

            // 2️⃣ Get the current node by matching its AvevaTag (or other unique link)
            var currentNode = pgNodes.FirstOrDefault(n =>
                string.Equals(XElementAttributeHelper.GetAvevaTag(n.Node), dto.AvevaTag, StringComparison.OrdinalIgnoreCase));

            if (currentNode == null)
                return null;

            // 3️⃣ Get the parent XElement
            var parentNode = currentNode.Node?.Parent;
            if (parentNode == null || parentNode.Name.LocalName.Equals("Root", StringComparison.OrdinalIgnoreCase))
                return null;

            // 4️⃣ Find the PGNode whose Node matches that parent XElement
            var parentPgNode = pgNodes.FirstOrDefault(n => n.Node == parentNode);
            if (parentPgNode == null)
                return null;

            // 5️⃣ Match the parent DTO by its AvevaTag (or another unique key)
            var parentAvevaTag = XElementAttributeHelper.GetAvevaTag(parentPgNode.Node);
            if (string.IsNullOrEmpty(parentAvevaTag))
                return null;

            return contract.Items.FirstOrDefault(x =>
                string.Equals(x.AvevaTag, parentAvevaTag, StringComparison.OrdinalIgnoreCase));
        }

        private void ConsolidateHierarchy(PipelineContract<ProjectStructureDTO> contract)
        {
            Console.WriteLine("🏗 [Process] Consolidating hierarchy across all DTOs...");

            var hierarchyResults = contract.Items
                .SelectMany(d => d.EngineResults.OfType<HierarchyEngineResult>())
                .ToList();

            if (!hierarchyResults.Any())
            {
                Console.WriteLine("⚠️ No HierarchyEngineResults found — skipping consolidation.");
                return;
            }

            var consolidationHandler = new HierarchyConsolidationHandler();
            contract.ConsolidatedTree = consolidationHandler.Consolidate(hierarchyResults);
            contract.ConsolidatedStructure = contract.ConsolidatedTree.Roots
                .SelectMany(FlattenTree)
                .OrderBy(n => n.Depth)
                .ToList();

            Console.WriteLine($"✅ Hierarchy consolidation complete: {contract.ConsolidatedStructure.Count} nodes total.");

            static IEnumerable<HierarchyNode> FlattenTree(HierarchyNode node)
            {
                yield return node;
                foreach (var child in node.Children.SelectMany(FlattenTree))
                    yield return child;
            }
        }

    }
}
