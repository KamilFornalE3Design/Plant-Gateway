using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using PlantGateway.Application.Pipelines.Execution.Writer.Interfaces;

namespace PlantGateway.Application.Pipelines.Execution.Writer.Strategies.TakeOverPoint
{
    /// <summary>
    /// Console writer for TakeOverPointDTO entries.
    /// Displays TakeOverPoint data and engine results in the console.
    /// </summary>
    public sealed class TakeOverPointConsoleWriter : IWriterStrategy<TakeOverPointDTO>
    {
        private readonly PipelineContract<TakeOverPointDTO> _pipelineContract;

        private const string Header = "NAME|GEOMETRYTYPE|CATREF|AVEVATAG|POSITION|ORIENTATION";

        public TakeOverPointConsoleWriter(PipelineContract<TakeOverPointDTO> pipelineContract)
        {
            _pipelineContract = pipelineContract ?? throw new ArgumentNullException(nameof(pipelineContract));
        }

        /// <summary>
        /// Writes all TakeOverPointDTO entries from the pipeline to the console.
        /// </summary>
        public void Write(PipelineContract<TakeOverPointDTO> pipelineContract)
        {
            var contract = pipelineContract ?? _pipelineContract;
            var dtos = contract.Items ?? new List<TakeOverPointDTO>();

            try
            {
                WriteEntries(dtos);
                Console.WriteLine($"✅ Displayed {dtos.Count()} TakeOverPoint entries");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ Failed to display TakeOverPoint entries: {ex.Message}");
            }
        }

        #region Private Writers

        /// <summary>
        /// Writes all formatted entries to the console.
        /// </summary>
        private static void WriteEntries(IEnumerable<TakeOverPointDTO> dtos)
        {
            var lines = dtos
                .Select(dto =>
                {
                    var name = string.Empty;
                    var geometryType = dto.EngineResults.OfType<RoleEngineResult>().FirstOrDefault()?.AvevaType ?? string.Empty;
                    var catref = dto.EngineResults.OfType<CatrefEngineResult>().FirstOrDefault()?.Catref ?? string.Empty;
                    var avevaTag = string.Empty;
                    var position = dto.EngineResults.OfType<PositionEngineResult>().FirstOrDefault()?.Position ?? string.Empty;
                    var orientation = dto.EngineResults.OfType<OrientationEngineResult>().FirstOrDefault()?.Orientation ?? string.Empty;

                    return $"{name}|{geometryType}|{catref}|{avevaTag}|{position}|{orientation}";
                })
                .Prepend(Header);

            Console.WriteLine(string.Join(Environment.NewLine, lines));
        }

        #endregion
    }
}
