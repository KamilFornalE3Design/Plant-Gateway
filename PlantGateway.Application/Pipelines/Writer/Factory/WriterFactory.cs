using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using PlantGateway.Application.Pipelines.Writer.Interfaces;
using PlantGateway.Application.Pipelines.Writer.Strategies.ProjectStructure;
using PlantGateway.Application.Pipelines.Writer.Strategies.TakeOverPoint;

namespace PlantGateway.Application.Pipelines.Writer.Factory
{
    /// <summary>
    /// Factory for resolving writer strategies based on DTO type, sink type and output format.
    /// </summary>
    public class WriterFactory : IWriterFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigProvider _configProvider;

        public WriterFactory(IServiceProvider serviceProvider, IConfigProvider configProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public IWriterStrategy<TDto> Create<TDto>(PipelineContract<TDto> pipelineContract)
        {
            if (pipelineContract == null)
                throw new ArgumentNullException(nameof(pipelineContract));

            var output = pipelineContract.Output
                         ?? throw new InvalidOperationException("Output target is missing in PipelineContract.");

            // --- Step 1: DTO type ---
            if (typeof(TDto) == typeof(TakeOverPointDTO))
                return (IWriterStrategy<TDto>)ResolveTakeOverPoint((PipelineContract<TakeOverPointDTO>)(object)pipelineContract);

            if (typeof(TDto) == typeof(ProjectStructureDTO))
                return (IWriterStrategy<TDto>)ResolveProjectStructure((PipelineContract<ProjectStructureDTO>)(object)pipelineContract);


            throw new NotSupportedException($"Unsupported DTO type {typeof(TDto).Name} for WriterFactory.");
        }

        // ──────────────────────────────────────────────────────────────
        // TakeOverPoint resolution
        // ──────────────────────────────────────────────────────────────
        private object ResolveTakeOverPoint(PipelineContract<TakeOverPointDTO> contract)
        {
            var output = contract.Output;

            switch (output.SinkType)
            {
                case OutputSinkType.Console:
                    return new TakeOverPointConsoleWriter(contract);

                case OutputSinkType.File:
                    if (!output.Paths.TryGetValue("File", out var fileDef))
                        throw new InvalidOperationException("No file output path defined for TakeOverPoint.");

                    return fileDef.DataFormat switch
                    {
                        OutputDataFormat.txt => new TakeOverPointTxtWriter(_serviceProvider, _configProvider),
                        //OutputDataFormat.json => new TakeOverPointJsonWriter(contract),
                        //OutputDataFormat.xml => new TakeOverPointXmlWriter(contract),
                        //OutputDataFormat.pmlmac => new TakeOverPointPmlmacWriter(contract),
                        _ => throw new NotSupportedException(
                            $"Unsupported file format {fileDef.DataFormat} for TakeOverPointDTO.")
                    };

                case OutputSinkType.Db:
                    throw new NotSupportedException("DB sink not yet supported for TakeOverPoint.");

                case OutputSinkType.API:
                    throw new NotSupportedException("API sink not yet supported for TakeOverPoint.");

                default:
                    throw new NotSupportedException(
                        $"Unsupported sink type {output.SinkType} for TakeOverPointDTO.");
            }
        }

        // ──────────────────────────────────────────────────────────────
        // ProjectStructure resolution
        // ──────────────────────────────────────────────────────────────
        private object ResolveProjectStructure(PipelineContract<ProjectStructureDTO> contract)
        {
            var output = contract.Output;

            switch (output.SinkType)
            {
                case OutputSinkType.Console:
                    return new ProjectStructureConsoleWriter(_serviceProvider, _configProvider);

                case OutputSinkType.File:
                    if (!output.Paths.TryGetValue("File", out var fileDef))
                        throw new InvalidOperationException("No file output path defined for ProjectStructure.");

                    return fileDef.DataFormat switch
                    {
                        OutputDataFormat.txt => new ProjectStructureTxtWriter(_serviceProvider, _configProvider),
                        //OutputDataFormat.json => new ProjectStructureJsonWriter(contract),
                        OutputDataFormat.xml => new ProjectStructureXmlWriter(_serviceProvider, _configProvider),
                        //OutputDataFormat.pmlmac => new ProjectStructurePmlmacWriter(contract),
                        _ => throw new NotSupportedException(
                            $"Unsupported file format {fileDef.DataFormat} for ProjectStructureDTO.")
                    };

                case OutputSinkType.Db:
                    throw new NotSupportedException("DB sink not yet supported for ProjectStructure.");

                case OutputSinkType.API:
                    throw new NotSupportedException("API sink not yet supported for ProjectStructure.");

                default:
                    throw new NotSupportedException(
                        $"Unsupported sink type {output.SinkType} for ProjectStructureDTO.");
            }
        }
    }
}
