using PlantGateway.Application.Pipelines.Execution.Planner.Interfaces;
using PlantGateway.Application.Pipelines.Planner;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Utilities.Planner.Interfaces;
using SMSgroup.Aveva.Utilities.Planner.Strategies;

namespace PlantGateway.Application.Pipelines.Execution.Planner.Factories
{
    public class PlannerFactory : IPlannerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigProvider _configProvider;

        public PlannerFactory(IServiceProvider serviceProvider, IConfigProvider configProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public IPlanner<TDto> Create<TDto>(PipelineContract<TDto> pipelineContract)
        {
            return new CommonPlannerStrategy<TDto>(_serviceProvider, _configProvider);
        }
    }
}
