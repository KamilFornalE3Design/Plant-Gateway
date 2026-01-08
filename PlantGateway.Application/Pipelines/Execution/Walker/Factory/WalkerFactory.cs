using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Data.IdentityCache;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using PlantGateway.Application.Pipelines.Walker.Interfaces;
using PlantGateway.Application.Pipelines.Walker.Strategies;
using PlantGateway.Application.Pipelines.Walker;
using PlantGateway.Application.Pipelines.Execution.Walker.Interfaces;

namespace PlantGateway.Application.Pipelines.Execution.Walker.Factory
{
    /// <summary>
    /// Factory responsible for creating walker strategies based on DTO type and input target.
    /// </summary>
    public class WalkerFactory : IWalkerFactory
    {
        private readonly IConfigProvider _configProvider;
        private readonly IHeaderMapService _headerMapService;
        private readonly ICatrefMapService _catrefMapService;

        private readonly TakeOverPointCacheService _takeOverPointCacheService;

        public WalkerFactory(IConfigProvider configProvider, IHeaderMapService headerMapService, ICatrefMapService catrefMapService, TakeOverPointCacheService takeOverPointCacheService)
        {
            _configProvider = configProvider;
            _headerMapService = headerMapService;
            _catrefMapService = catrefMapService;

            _takeOverPointCacheService = takeOverPointCacheService;
        }

        /// <summary>
        /// Creates a walker strategy for the specified DTO type and input target.
        /// </summary>
        public IWalkerStrategy<TDto> Create<TDto>(PipelineContract<TDto> pipelineContract)
        {
            if (typeof(TDto) == typeof(TakeOverPointDTO))
            {
                switch (pipelineContract.Input.Format)
                {
                    case InputDataFormat.txt:
                        return (IWalkerStrategy<TDto>)(object)
                            new TxtWalkerStrategy(_configProvider, _headerMapService, _catrefMapService, _takeOverPointCacheService);

                    case InputDataFormat.xml:
                        return (IWalkerStrategy<TDto>)(object)
                            new XmlWalkerStrategy(_configProvider);
                }
            }
            else if (typeof(TDto) == typeof(ProjectStructureDTO))
            {
                switch (pipelineContract.Input.Format)
                {
                    case InputDataFormat.xml:
                        return (IWalkerStrategy<TDto>)(object)
                            new XmlWalkerStrategy(_configProvider);
                }
            }

            throw new NotSupportedException($"Unsupported walker: {typeof(TDto).Name} in format {pipelineContract.Input.Format}");
        }
    }
}
