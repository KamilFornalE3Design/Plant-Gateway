using Microsoft.Extensions.DependencyInjection;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using PlantGateway.Application.Pipelines.Parser.Interfaces;

namespace PlantGateway.Application.Pipelines.Parser.Factories
{
    public sealed class ParserFactory : IParserFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigProvider _configProvider;

        public ParserFactory(IServiceProvider provider, IConfigProvider configProvider)
        {
            _serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public IParserStrategy Create<TDto>(PipelineContract<TDto> pipelineContract)
        {
            if (pipelineContract == null)
                throw new ArgumentNullException(nameof(pipelineContract));
            if (pipelineContract.Input == null)
                throw new ArgumentException("PipelineContract.Input cannot be null", nameof(pipelineContract));

            var format = pipelineContract.Input.Format;

            // Get all registered non-generic parser strategies
            var parsers = _serviceProvider.GetRequiredService<IEnumerable<IParserStrategy>>();

            // Select parser by format (e.g. txt, xml)
            var parser = parsers.FirstOrDefault(p => p.Format == format);

            if (parser == null)
                throw new NotSupportedException($"No parser registered for format '{format}'.");

            return parser;
        }
    }
}
