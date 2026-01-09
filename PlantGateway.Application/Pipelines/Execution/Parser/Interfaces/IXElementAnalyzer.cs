using PlantGateway.Application.Pipelines.Results.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Execution.Parser.Interfaces
{
    /// <summary>
    /// XML element analyzer: operates on XElement and implements the common IElementAnalyzer.
    /// </summary>
    public interface IXElementAnalyzer : IElementAnalyzer, IAnalyzer<XElement>
    {
        /// <summary>
        /// Returns true if this analyzer wants to handle the given element.
        /// </summary>
        bool CanHandle(XElement xElement);

        /// <summary>
        /// Typed analysis entry point for XElement.
        /// </summary>
        void Analyze(XElement xElement, ParserResult result);
    }
}
