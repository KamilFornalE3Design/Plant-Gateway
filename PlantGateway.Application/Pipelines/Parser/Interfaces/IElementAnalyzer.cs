using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Application.Pipelines.Parser.Interfaces
{
    /// <summary>
    /// Analyzer that operates on an element-level artifact
    /// (XML element, DB row, JSON node, etc.).
    /// Untyped common base for format-specific element analyzers.
    /// </summary>
    public interface IElementAnalyzer
    {
        bool CanHandle(object element);

        void Analyze(object element, ParserResult result);
    }
}
