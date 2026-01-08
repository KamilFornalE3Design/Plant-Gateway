using Microsoft.Extensions.DependencyInjection;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using PlantGateway.Application.Pipelines.Parser;
using PlantGateway.Application.Pipelines.Execution.Validator.Interfaces;

namespace PlantGateway.Application.Pipelines.Execution.Validator.Factories
{
    /// <summary>
    /// Default implementation of <see cref="IValidatorFactory"/>.
    /// Selects the appropriate validator strategy
    /// according to the input target format (e.g. xml, txt).
    /// </summary>
    public sealed class ValidatorFactory : IValidatorFactory
    {
        private readonly IConfigProvider _configProvider;
        private readonly IServiceProvider _serviceProvider;

        public ValidatorFactory(IServiceProvider serviceProvider, IConfigProvider configProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public IValidatorStrategy Create<TDto>(PipelineContract<TDto> pipelineContract)
        {
            if (pipelineContract == null)
                throw new ArgumentNullException(nameof(pipelineContract));
            if (pipelineContract.Input == null)
                throw new ArgumentException("PipelineContract.Input cannot be null", nameof(pipelineContract));

            var format = pipelineContract.Input.Format;

            // Get all registered non-generic validator strategies
            var validators = _serviceProvider.GetRequiredService<IEnumerable<IValidatorStrategy>>();

            // Select validator by format (e.g. txt, xml)
            var validator = validators.FirstOrDefault(p => p.Format == format);

            if (validator == null)
                throw new NotSupportedException($"No validator registered for format '{format}'.");

            return validator;
        }
    }
}
