using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using System.Text;
using PlantGateway.Application.Pipelines.Execution.Writer.Interfaces;

namespace PlantGateway.Application.Pipelines.Execution.Writer.Strategies.TakeOverPoint
{/// <summary>
 /// TXT writer for TakeOverPointDTO entries.
 /// Exports TakeOverPoint data to a delimited text file.
 /// </summary>
    public sealed class TakeOverPointTxtWriter : IWriterStrategy<TakeOverPointDTO>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigProvider _configProvider;

        private const string Header = "NAME|GEOMETRYTYPE|CATREF|AVEVATAG|POSITION|ORIENTATION";

        public TakeOverPointTxtWriter(IServiceProvider serviceProvider, IConfigProvider configProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        /// <summary>
        /// Writes all TakeOverPointDTO entries to a text file.
        /// </summary>
        public void Write(PipelineContract<TakeOverPointDTO> pipelineContract)
        {
            var contract = pipelineContract;
            var target = contract.Output;
            var dtos = contract.Items ?? Enumerable.Empty<TakeOverPointDTO>();

            if (target == null)
                throw new InvalidOperationException("Missing OutputTarget in PipelineContract.");

            if (!target.Paths.TryGetValue("File", out var fileDef))
                throw new InvalidOperationException("No file path defined in OutputTarget for TakeOverPoint.");

            if (!fileDef.Identifiers.TryGetValue("All", out var id))
                throw new InvalidOperationException("No 'All' identifier defined in OutputTarget for TakeOverPoint.");

            var filePath = id.Value;

            try
            {
                var lines = dtos
                    .Select(dto =>
                    {
                        var name = dto.EngineResults.OfType<TagEngineResult>().FirstOrDefault()?.FullTag ?? string.Empty;
                        var geometryType = dto.EngineResults.OfType<RoleEngineResult>().FirstOrDefault()?.AvevaType ?? string.Empty;
                        var catref = dto.EngineResults.OfType<CatrefEngineResult>().FirstOrDefault()?.Catref ?? string.Empty;
                        var avevaTag = dto.AvevaTag ?? string.Empty;
                        var position = dto.EngineResults.OfType<PositionEngineResult>().FirstOrDefault()?.Position ?? string.Empty;
                        var orientation = dto.EngineResults.OfType<OrientationEngineResult>().FirstOrDefault()?.Orientation ?? string.Empty;

                        return $"{name}|{geometryType}|{catref}|{avevaTag}|{position}|{orientation}";
                    })
                    .Prepend(Header);

                File.WriteAllLines(filePath, lines, Encoding.UTF8);

                Console.WriteLine($"✅ TakeOverPoint TXT written to {filePath}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ Failed to write TakeOverPoint TXT: {ex.Message}");
            }
        }
    }
}
