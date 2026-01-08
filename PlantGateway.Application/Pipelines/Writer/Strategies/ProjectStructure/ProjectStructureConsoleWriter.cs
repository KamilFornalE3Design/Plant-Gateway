using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using PlantGateway.Application.Pipelines.Writer.Interfaces;

namespace PlantGateway.Application.Pipelines.Writer.Strategies.ProjectStructure
{
    /// <summary>
    /// Console writer for ProjectStructureDTO entries.
    /// Mirrors the text writer logic but outputs to console instead of file.
    /// </summary>
    public sealed class ProjectStructureConsoleWriter : IWriterStrategy<ProjectStructureDTO>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigProvider _configProvider;

        private static readonly string StructureHeader = "OWNER_AVEVA_TYPE|OWNER_AVEVA_TAG|AVEVA_TYPE|AVEVA_TAG";
        private static readonly string ImportHeader = "AVEVA_TAG|FILE";
        private static readonly string AttributesHeader = "AVEVA_TAG|POSITION|ORIENTATION";

        public ProjectStructureConsoleWriter(IServiceProvider serviceProvider, IConfigProvider configProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        /// <summary>
        /// Writes the project structure data to the console, section by section.
        /// </summary>
        public void Write(PipelineContract<ProjectStructureDTO> pipelineContract)
        {
            var contract = pipelineContract;
            var target = contract.Output;
            var dtos = contract.Items ?? new List<ProjectStructureDTO>();

            Console.WriteLine("=== ProjectStructure entries ===");
            Console.WriteLine();

            try
            {
                WriteStructureSection(dtos);
                WriteImportSection(dtos);
                WriteAttributesSection(dtos);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ Failed to display ProjectStructure: {ex.Message}");
            }

            Console.WriteLine("=== End of ProjectStructure entries ===");
        }

        // ──────────────────────────────────────────────────────────────
        // Individual section writers
        // ──────────────────────────────────────────────────────────────
        private static void WriteStructureSection(IEnumerable<ProjectStructureDTO> dtos)
        {
            //var lines = dtos.Select(dto =>
            //{
            //    var hierarchy = dto.EngineResults.OfType<HierarchyEngineResult>().FirstOrDefault();
            //    var tag = dto.EngineResults.OfType<TagEngineResult>().FirstOrDefault();

            //    var ownerAvevaType = hierarchy?.OwnerAvevaType;
            //    var ownerAvevaTag = string.Empty; // nie wiem gdzie to ma być, może hierarchy?
            //    var avevaType = hierarchy?.AvevaType;
            //    var avevaTag = tag?.AvevaTag ?? string.Empty;

            //    return $"{ownerAvevaType}|{ownerAvevaTag}|{avevaType}|{avevaTag}";
            //})
            //.Prepend(StructureHeader)
            //.Prepend("[Structure]");

            //Console.WriteLine(string.Join(Environment.NewLine, lines));
            //Console.WriteLine();
        }

        private static void WriteImportSection(IEnumerable<ProjectStructureDTO> dtos)
        {
            //var lines = dtos
            //    .Where(dto => dto.EngineResults.OfType<HierarchyEngineResult>().FirstOrDefault()?.AvevaType == AvevaHierarchyRole.EQUI)
            //    .Select(dto =>
            //    {
            //        var tag = dto.EngineResults.OfType<TagEngineResult>().FirstOrDefault();
            //        var avevaTag = tag?.AvevaTag ?? string.Empty;
            //        return $"{avevaTag}|{dto.FileFullPath}";
            //    })
            //    .Prepend(ImportHeader)
            //    .Prepend("[Import]");

            //Console.WriteLine(string.Join(Environment.NewLine, lines));
            //Console.WriteLine();
        }

        private static void WriteAttributesSection(IEnumerable<ProjectStructureDTO> dtos)
        {
            //var lines = dtos.Select(dto =>
            //{
            //    var tag = dto.EngineResults.OfType<TagEngineResult>().FirstOrDefault();
            //    var position = dto.EngineResults.OfType<PositionEngineResult>().FirstOrDefault();
            //    var orientation = dto.EngineResults.OfType<OrientationEngineResult>().FirstOrDefault();

            //    var avevaTag = tag?.AvevaTag ?? string.Empty;
            //    var pos = position?.Position ?? string.Empty;
            //    var ori = orientation?.Orientation ?? string.Empty;

            //    return $"{avevaTag}|{pos}|{ori}";
            //})
            //.Prepend(AttributesHeader)
            //.Prepend("[Attributes]");

            //Console.WriteLine(string.Join(Environment.NewLine, lines));
            //Console.WriteLine();
        }

    }
}
