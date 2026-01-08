using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Planner;
using SMSgroup.Aveva.Config.Models.PlannerBlocks.Position;
using SMSgroup.Aveva.Utilities.Planner.Interfaces;

namespace PlantGateway.Application.Pipelines.Planner.Strategies
{
    /// <summary>
    /// Common planner for simple schema resolution.
    /// </summary>
    public sealed class CommonPlannerStrategy<TDto> : IPlanner<TDto>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigProvider _configProvider;

        public CommonPlannerStrategy(IServiceProvider serviceProvider, IConfigProvider configProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public PlannerResult BuildPlan(PipelineContract<TDto> pipelineContract, bool dryRun = true)
        {
            if (pipelineContract == null)
                throw new ArgumentNullException(nameof(pipelineContract));

            var strategyPlan = new PlannerResult();

            // Build strategy plan - for now anyting to mark idea
            StrategyPlanSamples(pipelineContract, strategyPlan);
            StrategyPlanPosition(pipelineContract, strategyPlan);
            StrategyPlanOrientation(pipelineContract, strategyPlan);

            if (dryRun)
            {
                // Wywaliłem bo to żadna informacja dla użytkownika
                // Console.WriteLine(strategyPlan.Describe());
            }

            return strategyPlan;
        }

        private void StrategyPlanSamples(PipelineContract<TDto> pipelineContract, PlannerResult strategyPlan)
        {
            strategyPlan.Schema = pipelineContract.ParserResult.DetectedInputSchema.ToString(); // Placeholder, no use
        }
        private void StrategyPlanPosition(PipelineContract<TDto> pipelineContract, PlannerResult strategyPlan)
        {
            strategyPlan.CsysOption = CsysOption.Relative; // Default option waiting for better input
            strategyPlan.CsysReference = new CsysReferenceOffset(); // Default refernece, so origins are (0,0,0)
            strategyPlan.CsysReference = new CsysReferenceOffset(); // Default refernece, so origins are (0,0,0)
        }
        private void StrategyPlanOrientation(PipelineContract<TDto> pipelineContract, PlannerResult strategyPlan)
        {
            // Placeholder
        }
    }
}
