using System;
using System.Collections.Generic;

namespace PlantGateway.Domain.Services.Engines.Abstractions
{
    /// <summary>
    /// Shared diagnostic and lifecycle contract for all engine results.
    /// Enables cross-engine analysis, diagnostics, and export.
    /// </summary>
    public interface IEngineResult
    {
        /// <summary>
        /// Identifier of the DTO that produced this engine result.
        /// </summary>
        Guid SourceDtoId { get; set; }

        /// <summary>
        /// Indicates whether the engine completed successfully.
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// Optional validity flag; often equal to IsSuccess but may differ.
        /// </summary>
        bool IsValid { get; set; }

        /// <summary>
        /// Informational messages from the engine (non-blocking).
        /// </summary>
        List<string> Message { get; set; }

        /// <summary>
        /// Warnings issued during processing (soft failures).
        /// </summary>
        List<string> Warning { get; set; }

        /// <summary>
        /// Hard errors that prevent successful result.
        /// </summary>
        List<string> Error { get; set; }

        /// <summary>
        /// Helper methods to append diagnostics safely.
        /// </summary>
        void AddMessage(string text);
        void AddWarning(string text);
        void AddError(string text);
    }
}
