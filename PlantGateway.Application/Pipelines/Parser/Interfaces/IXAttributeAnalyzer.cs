using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Parser.Interfaces
{
    /// <summary>
    /// XML attribute analyzer: operates on XAttribute and implements the common IAttributeAnalyzer.
    /// </summary>
    public interface IXAttributeAnalyzer : IAttributeAnalyzer, IAnalyzer<XAttribute>
    {
        /// <summary>
        /// Returns true if this analyzer wants to handle the given attribute.
        /// </summary>
        bool CanHandle(XAttribute xAttribute);

        /// <summary>
        /// Typed analysis entry point for XAttribute.
        /// </summary>
        void Analyze(XAttribute xAttribute, ParserResult result);
    }
}
