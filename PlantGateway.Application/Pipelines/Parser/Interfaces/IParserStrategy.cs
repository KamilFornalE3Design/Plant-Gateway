using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using SMSgroup.Aveva.Config.Models.ValueObjects;

namespace PlantGateway.Application.Pipelines.Parser.Interfaces
{
    /// <summary>
    /// Base, non-generic parser strategy interface.
    /// Used by factories and coordinators to discover parsers
    /// without knowing the DTO type.
    /// </summary>
    public interface IParserStrategy
    {
        /// <summary>
        /// File input format handled by this parser (e.g. txt, xml, csv).
        /// </summary>
        InputDataFormat Format { get; }

        /// <summary>
        /// Performs lightweight analysis of the input file and produces a ParserResult.
        /// This method is DTO-agnostic — used for file inspection and flag deduction.
        /// </summary>
        ParserResult Analyze(IPipelineContract pipeline);
    }

    /// <summary>
    /// Typed parser interface for scenarios where the DTO type is known.
    /// Provides type-safe access to the pipeline contract and results.
    /// </summary>
    public interface IParserStrategy<TDto> : IParserStrategy
    {
        /// <summary>
        /// Strongly-typed variant of <see cref="Analyze"/> that accepts
        /// a typed pipeline contract for DTO-specific downstream processing.
        /// </summary>
        ParserResult Analyze(PipelineContract<TDto> pipeline);
    }
}
