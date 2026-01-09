using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SMSgroup.Aveva.Config.Models.Disposition;

namespace PlantGateway.Domain.Services.Engines.Abstractions
{
    /// <summary>
    /// Contract for a single disposition stage in the DispositionEngine pipeline.
    /// </summary>
    internal interface IDispositionStage
    {
        /// <summary>
        /// Logical stage identifier (for ordering / diagnostics).
        /// </summary>
        DispositionStageId Id { get; }

        /// <summary>
        /// Human-readable stage name, useful for diagnostics and logging.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executes the stage logic against the given context.
        /// Implementations can read and mutate the context.
        /// </summary>
        /// <param name="context">Current disposition context.</param>
        void Execute(DispositionContext context);
    }
}
