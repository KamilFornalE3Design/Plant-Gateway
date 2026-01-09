using SMSgroup.Aveva.Config.Models.Tokenization;

namespace PlantGateway.Domain.Services.Tokenization.Stages
{
    /// <summary>
    /// Represents a single step in the TokenEngine internal pipeline.
    /// 
    /// Implementations:
    /// - Are stateless (or treat state as read-only configuration).
    /// - Mutate the provided <see cref="TokenizationContext"/>.
    /// - Must not throw for normal parsing issues; instead, record
    ///   messages/warnings/errors in the context.
    /// </summary>
    public interface ITokenizationStage
    {
        /// <summary>
        /// Logical identifier of this stage in the tokenization pipeline.
        /// Used by TokenEngine to order and/or stop execution at a given step.
        /// </summary>
        TokenizationStageId Id { get; }

        /// <summary>
        /// Executes the stage logic against the given context.
        /// The stage may read and mutate the context but should
        /// not replace it with a new instance.
        /// </summary>
        /// <param name="context">The working state for tokenization.</param>
        void Execute(TokenizationContext context);
    }
}
