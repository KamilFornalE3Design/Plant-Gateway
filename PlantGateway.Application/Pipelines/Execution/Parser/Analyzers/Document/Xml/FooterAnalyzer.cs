using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using SMSgroup.Aveva.Utilities.Parser.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Execution.Parser.Analyzers.Document.Xml
{
    /// <summary>
    /// Document-level analyzer for "footer" / trailing sections, if present.
    /// Currently acts as a placeholder for future global stats / summaries.
    /// </summary>
    public sealed class FooterAnalyzer : IXDocumentAnalyzer
    {
        public bool CanHandle(XDocument xDocument)
        {
            // For now: any Root doc is acceptable; refine later if you add footer semantics.
            return xDocument.Root != null;
        }

        public void Analyze(XDocument xDocument, ParserResult result)
        {
            // TODO: later aggregate global statistics into ParserStepResult
            // (e.g. from ParserHints filled by other analyzers).
        }

        bool IDocumentAnalyzer.CanHandle(object document) =>
            document is XDocument doc && CanHandle(doc);

        void IDocumentAnalyzer.Analyze(object document, ParserResult result)
        {
            if (document is XDocument doc)
            {
                Analyze(doc, result);
            }
        }

        void IAnalyzer<XDocument>.Analyze(XDocument input, ParserResult result) =>
            Analyze(input, result);
    }
}
