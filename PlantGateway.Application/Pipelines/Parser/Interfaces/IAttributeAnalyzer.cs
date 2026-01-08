using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Application.Pipelines.Parser.Interfaces
{
    /// <summary>
    /// Analyzer that operates on an attribute-level artifact
    /// (XML attribute, DB column value, JSON property, etc.).
    /// Untyped common base for format-specific attribute analyzers.
    /// </summary>
    public interface IAttributeAnalyzer
    {
        bool CanHandle(object attribute);

        void Analyze(object attribute, ParserResult result);
    }
}
