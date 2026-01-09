namespace PlantGateway.Application.Pipelines.Orchestration
{
    /// <summary>
    /// Defines the available workflow phases in the PGedge pipeline.
    /// Used by CLI and orchestrators to decide which processing stages to execute.
    /// </summary>
    public enum ProcessPhase
    {
        /// <summary>Run only the Parser phase.</summary>
        Parse = 0,

        /// <summary>Run Parser + Validator phases.</summary>
        Validate = 1,

        /// <summary>Run Parser + Validator + Planner phases.</summary>
        Plan = 2,

        /// <summary>Run all phases (Parser → Validator → Planner → Execution).</summary>
        Full = 3
    }
}
