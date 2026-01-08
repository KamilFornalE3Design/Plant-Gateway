using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Planner;

namespace PlantGateway.Application.Pipelines.Execution.Planner.Interfaces
{
    public interface IPlanner<TDto>
    {
        PlannerResult BuildPlan(PipelineContract<TDto> pipeline, bool dryRun = true);
    }
}
