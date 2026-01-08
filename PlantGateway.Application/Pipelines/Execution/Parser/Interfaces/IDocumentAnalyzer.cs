using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Application.Pipelines.Execution.Parser.Interfaces
{
    /// <summary>
    /// Analyzer that operates on a document-level artifact (XML file, DB dataset, etc.).
    /// Untyped. Used as a common base for format-specific document analyzers.
    /// </summary>
    public interface IDocumentAnalyzer
    {
        /// <summary>
        /// Returns true if this analyzer wants to handle the given document object.
        /// </summary>
        bool CanHandle(object document);

        /// <summary>
        /// Performs analysis on the given document object.
        /// </summary>
        void Analyze(object document, ParserResult result);
    }
}
