using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Planner;
using PlantGateway.Core.Config.Models.PlannerBlocks;
using SMSgroup.Aveva.Utilities.Planner.Interfaces;
using PlantGateway.Application.Pipelines.Execution.Planner.Interfaces;

namespace PlantGateway.Application.Pipelines.Execution.Planner.Strategies
{
    /// <summary>
    /// Planner strategy dedicated to TakeOverPoint pipelines.
    /// 
    /// Responsible for translating parser results and input format
    /// into a structured <see cref="PlannerResult"/> used by subsequent
    /// handlers and executors.
    /// 
    /// Currently acts as a placeholder; core logic will be implemented
    /// once schema-specific plan rules are defined.
    /// </summary>
    /// <typeparam name="TDto">
    /// Expected to be <see cref="TakeOverPointDTO"/>. 
    /// Type parameter retained for consistency with planner interfaces.
    /// </typeparam>
    public sealed class TakeOverPointPlannerStrategy<TDto> : IPlanner<TDto> where TDto : IPlantGatewayDTO
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigProvider _configProvider;

        public TakeOverPointPlannerStrategy(IServiceProvider serviceProvider, IConfigProvider configProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        /// <summary>
        /// Builds a basic <see cref="PlannerResult"/> for TakeOverPoint input.
        /// Currently returns an empty plan placeholder.
        /// </summary>
        /// <param name="pipeline">Pipeline contract for the current run.</param>
        /// <param name="dryRun">
        /// If true, planner performs dry analysis without binding execution handlers.
        /// </param>
        /// <returns>A new, uninitialized <see cref="PlannerResult"/>.</returns>
        public PlannerResult BuildPlan(PipelineContract<TDto> pipelineContract, bool dryRun = true)
        {
            if (pipelineContract == null)
                throw new ArgumentNullException(nameof(pipelineContract));

            var plannerResult = new PlannerResult
            {
                Name = "TakeOverPoint Plan (placeholder)",
                Description = "Initial plan structure for TakeOverPoint processing; no executable steps defined yet.",
                IsSuccess = true,
                Schema = (pipelineContract.ParserResult?.DetectedInputSchema ?? DetectedInputSchema.Unknown).ToString(),
                TargetDtoType = typeof(TakeOverPointDTO),

                // For now this is static; later planners will fill in dynamically.
                ExecutionSteps = new List<string>
                {
                    "LoadRawLines",
                    "NormalizeSeparators",
                    "ConvertToTable",
                    "CreateDtos"
                },

                ExecutionFlags = new Dictionary<string, bool>
                {
                    ["MergePrefixedTags"] = false,
                    ["SkipValidation"] = true
                },

                ExecutionParameters = new Dictionary<string, object>
                {
                    ["HeaderVersion"] = pipelineContract.ParserResult?.ParserHints.TryGetValue("HeaderVersion", out var version),
                    ["PlannerCreated"] = DateTime.UtcNow.ToString("O")
                },

                Warnings = new List<string>(),
                Summary = "Placeholder TakeOverPoint plan created successfully with default walker sequence.",
                CsysOption = CsysOption.Global,
                CsysReference = new CsysReferenceOffset(),
                CsysRelative = CsysWRT.Owner
            };

            return plannerResult;
        }
    }
}
