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
    /// XML document analyzer: operates on XDocument and implements the common IDocumentAnalyzer.
    /// </summary>
    public interface IXDocumentAnalyzer : IDocumentAnalyzer, IAnalyzer<XDocument>
    {
        /// <summary>
        /// Typed check if this analyzer wants to handle the given XDocument.
        /// </summary>
        bool CanHandle(XDocument xDocument);

        /// <summary>
        /// Typed analysis entry point for XDocument.
        /// </summary>
        void Analyze(XDocument xDocument, ParserResult result);
    }
}
