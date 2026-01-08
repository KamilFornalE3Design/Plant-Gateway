using Microsoft.Extensions.DependencyInjection;
using PlantGateway.Application.Pipelines.Execution.Processor.Interfaces;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Data.IdentityCache;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Utilities.Engines;
using SMSgroup.Aveva.Utilities.Processor.Interfaces;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Execution.Processor.Strategies
{
    /// <summary>
    /// Processor strategy for <see cref="TakeOverPointDTO"/>.
    /// Coordinates all enrichment engines (orientation, catref, naming, etc.)
    /// and stores their results into each DTO's EngineResults list.
    /// </summary>
    public sealed class TakeOverPointProcessorStrategy : IProcessorStrategy<TakeOverPointDTO>
    {
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        public TakeOverPointProcessorStrategy(IServiceProvider serviceProvider, IConfigProvider configProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public PipelineContract<TakeOverPointDTO> Process(PipelineContract<TakeOverPointDTO> contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));

            // === Resolve services ===
            var catrefMapService = _serviceProvider.GetRequiredService<ICatrefMapService>();
            var disciplineMapService = _serviceProvider.GetRequiredService<IDisciplineMapService>();
            var tosMapService = _serviceProvider.GetRequiredService<ITechnicalOrderStructureMapService>();
            var entityMapService = _serviceProvider.GetRequiredService<IEntityMapService>();
            var roleMapService = _serviceProvider.GetRequiredService<IRoleMapService>();
            var tokenregexMapService = _serviceProvider.GetRequiredService<ITokenRegexMapService>();
            var allowedTreeMapService = _serviceProvider.GetRequiredService<IAllowedTreeMapService>();
            var suffixMapService = _serviceProvider.GetRequiredService<ISuffixMapService>();
            var disciplineHierarchyTokenMapService = _serviceProvider.GetRequiredService<IDisciplineHierarchyTokenMapService>();
            var codificationMapService = _serviceProvider.GetRequiredService<ICodificationMapService>();
            var identityCacheService = _serviceProvider.GetRequiredService<TakeOverPointCacheService>();

            // === Instantiate engines ===
            var tokenEngine = new TokenEngine(disciplineHierarchyTokenMapService, tokenregexMapService, disciplineMapService, entityMapService, codificationMapService);
            var catrefEngine = new CatrefEngine(catrefMapService);
            var disciplineEngine = new DisciplineEngine(disciplineMapService);
            var roleEngine = new RoleEngine(roleMapService);
            var suffixEngine = new SuffixEngine(disciplineHierarchyTokenMapService, identityCacheService);
            var namingEngine = new NamingEngine(disciplineHierarchyTokenMapService, tokenregexMapService);
            var identityEngine = new IdentityEngine(identityCacheService);
            var tagEngine = new TagEngine();
            var hierarchyEngine = new HierarchyEngine(disciplineHierarchyTokenMapService, allowedTreeMapService);
            var transformationEngine = new TransformationEngine();
            var positionEngine = new PositionEngine(contract.CsysOption, contract.CsysWRT, contract.CsysReferenceOffset);
            var orientationEngine = new OrientationEngine(contract.CsysOption, contract.CsysWRT, contract.CsysReferenceOffset);

            // === Phase 1: PreProcess ===
            PreProcess(contract, tokenEngine);

            // === Phase 2: Main Processing ===
            ProcessMain(contract, roleEngine, suffixEngine, catrefEngine, disciplineEngine, namingEngine, identityEngine, tagEngine, hierarchyEngine);

            // === Phase 3: PostProcess ===
            PostProcess(contract, transformationEngine, positionEngine, orientationEngine);

            // === Diagnostics ===
            RunEngineCoverageDiagnostics(contract);

            return contract;
        }

        // ===============================================================
        // 🔹 PHASE 1 — PREPROCESS
        // ===============================================================
        private void PreProcess(PipelineContract<TakeOverPointDTO> contract, TokenEngine tokenEngine)
        {
            Console.WriteLine("🧩 [PreProcess] Tokenizing and preparing TakeOverPoint data...");

            foreach (var dto in contract.Items)
            {
                var tokenResult = tokenEngine.Process(dto);
                AddIfNotNull(dto, contract, tokenResult);
            }

            Console.WriteLine("✔ [PreProcess] Completed.");
        }

        // ===============================================================
        // 🔹 PHASE 2 — MAIN PROCESS
        // ===============================================================
        private void ProcessMain(
            PipelineContract<TakeOverPointDTO> contract,
            RoleEngine roleEngine,
            SuffixEngine suffixEngine,
            CatrefEngine catrefEngine,
            DisciplineEngine disciplineEngine,
            NamingEngine namingEngine,
            IdentityEngine identityEngine,
            TagEngine tagEngine,
            HierarchyEngine hierarchyEngine)
        {
            Console.WriteLine("⚙️ [ProcessMain] Starting enrichment...");

            foreach (var dto in contract.Items)
            {
                var roleResult = roleEngine.Process(dto);
                AddIfNotNull(dto, contract, roleResult);

                var suffixResult = suffixEngine.Process(dto, contract);
                AddIfNotNull(dto, contract, suffixResult);

                var catrefResult = catrefEngine.Process(dto);
                AddIfNotNull(dto, contract, catrefResult);

                var disciplineResult = disciplineEngine.Process(dto);
                AddIfNotNull(dto, contract, disciplineResult);

                var namingResult = namingEngine.Process(dto);
                AddIfNotNull(dto, contract, namingResult);

                var tagResult = tagEngine.Process(dto);
                AddIfNotNull(dto, contract, tagResult);

                var identityResult = identityEngine.Process(dto, contract);
                AddIfNotNull(dto, contract, identityResult);

                var hierarchyResult = hierarchyEngine.Process(dto, contract);
                AddIfNotNull(dto, contract, hierarchyResult);

                // Apply resolved ID from identity engine
                var identity = identityResult as IdentityEngineResult;
                if (identity != null)
                    dto.Id = identity.Id;
            }

            Console.WriteLine("✔ [ProcessMain] Completed enrichment.");
        }

        // ===============================================================
        // 🔹 PHASE 3 — POSTPROCESS
        // ===============================================================
        private void PostProcess(
            PipelineContract<TakeOverPointDTO> contract,
            TransformationEngine transformationEngine,
            PositionEngine positionEngine,
            OrientationEngine orientationEngine)
        {
            Console.WriteLine("📐 [PostProcess] Applying transformations, positions, and orientations...");

            foreach (var dto in contract.Items)
            {
                //AddIfNotNull(dto, contract, transformationEngine.Process(dto));
                AddIfNotNull(dto, contract, positionEngine.Process(dto, contract));
                AddIfNotNull(dto, contract, orientationEngine.Process(dto, contract));
            }

            Console.WriteLine("✔ [PostProcess] Completed transformations and position/orientation mapping.");
        }

        // ===============================================================
        // 🔹 UTILS
        // ===============================================================
        private static void AddIfNotNull(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract, IEngineResult? result)
        {
            if (result == null) return;
            dto.EngineResults.Add(result);
        }

        private void RunEngineCoverageDiagnostics(PipelineContract<TakeOverPointDTO> contract)
        {
            if (contract?.Items == null || contract.Items.Count == 0)
            {
                Console.WriteLine("⚠️ No DTOs available for diagnostics.");
                return;
            }

            var stats = contract.Items
                .Select(dto => new
                {
                    dto.Id,
                    dto.AvevaTag,
                    HasRole = dto.EngineResults.OfType<RoleEngineResult>().Any(),
                    HasSuffix = dto.EngineResults.OfType<SuffixEngineResult>().Any(),
                    HasDiscipline = dto.EngineResults.OfType<DisciplineEngineResult>().Any(),
                    HasNaming = dto.EngineResults.OfType<NamingEngineResult>().Any(),
                    HasIdentity = dto.EngineResults.OfType<IdentityEngineResult>().Any(),
                })
                .ToList();

            int total = stats.Count;
            int missingRole = stats.Count(x => !x.HasRole);
            int missingSuffix = stats.Count(x => !x.HasSuffix);
            int missingDisc = stats.Count(x => !x.HasDiscipline);
            int missingNaming = stats.Count(x => !x.HasNaming);
            int missingIdentity = stats.Count(x => !x.HasIdentity);

            Console.WriteLine("=== TAKEOVER POINT ENGINE COVERAGE SUMMARY ===");
            Console.WriteLine($"Total DTOs: {total}");
            Console.WriteLine($"Missing RoleEngineResult: {missingRole}");
            Console.WriteLine($"Missing SuffixEngineResult: {missingSuffix}");
            Console.WriteLine($"Missing DisciplineEngineResult: {missingDisc}");
            Console.WriteLine($"Missing NamingEngineResult: {missingNaming}");
            Console.WriteLine($"Missing IdentityEngineResult: {missingIdentity}");
            Console.WriteLine();

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
                            new XAttribute("MissingRole", missingRole),
                            new XAttribute("MissingSuffix", missingSuffix),
                            new XAttribute("MissingDiscipline", missingDisc),
                            new XAttribute("MissingNaming", missingNaming),
                            new XAttribute("MissingIdentity", missingIdentity),
                            new XElement("MissingItems",
                                stats
                                    .Where(x => !x.HasRole || !x.HasSuffix || !x.HasDiscipline || !x.HasNaming || !x.HasIdentity)
                                    .Select(x => new XElement("Item",
                                        new XAttribute("Id", x.Id),
                                        new XAttribute("AvevaTag", x.AvevaTag ?? "(null)"),
                                        new XAttribute("Role", x.HasRole),
                                        new XAttribute("Suffix", x.HasSuffix),
                                        new XAttribute("Discipline", x.HasDiscipline),
                                        new XAttribute("Naming", x.HasNaming),
                                        new XAttribute("Identity", x.HasIdentity)
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
    }
}
