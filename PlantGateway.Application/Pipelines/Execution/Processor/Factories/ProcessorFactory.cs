using PlantGateway.Application.Pipelines.Execution.Processor.Interfaces;
using PlantGateway.Application.Pipelines.Processor;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Utilities.Processor.Interfaces;
using SMSgroup.Aveva.Utilities.Processor.Strategies;

namespace PlantGateway.Application.Pipelines.Execution.Processor.Factories
{
    /// <summary>
    /// Factory responsible for creating processor strategies based on DTO type and input target.
    /// </summary>
    public class ProcessorFactory : IProcessorFactory
    {
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        public ProcessorFactory(IServiceProvider serviceProvider, IConfigProvider configProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        /// <summary>
        /// Creates a processor strategy for the specified DTO type and input target.
        /// </summary>
        public IProcessorStrategy<TDto> Create<TDto>(PipelineContract<TDto> pipeline)
        {
            if (typeof(TDto) == typeof(TakeOverPointDTO))
            {
                return (IProcessorStrategy<TDto>)(object)new TakeOverPointProcessorStrategy(_serviceProvider, _configProvider);
            }
            else if (typeof(TDto) == typeof(ProjectStructureDTO))
            {
                return (IProcessorStrategy<TDto>)(object)new ProjectStructureProcessorStrategy(_serviceProvider, _configProvider);
            }

            throw new NotSupportedException($"Unsupported processor: {typeof(TDto).Name} in format {pipeline.Input.Format}");
        }
    }
}
