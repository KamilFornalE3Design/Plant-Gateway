using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using SMSgroup.Aveva.Config.Models.ExecutionStepResult;
using SMSgroup.Aveva.Config.Models.ValueObjects;
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
    /// Document-level analyzer for the "body" of a project-structure XML:
    /// counts known nodes (Root/Header/Assembly/Part/etc.) and performs
    /// surface-level checks such as orphan Parts.
    /// </summary>
    public sealed class BodyAnalyzer : IXDocumentAnalyzer
    {
        private static readonly IReadOnlyDictionary<PGNodeKey, string> NodeNameMap =
            new Dictionary<PGNodeKey, string>
            {
                { PGNodeKey.Root,         "Root" },
                { PGNodeKey.Header,       "Header" },
                { PGNodeKey.Info,         "Info" },
                { PGNodeKey.Export,       "Export" },
                { PGNodeKey.Units,        "Units" },
                { PGNodeKey.Assembly,     "Assembly" },
                { PGNodeKey.Part,         "Part" },
                { PGNodeKey.Matrix,       "Matrix" },
                { PGNodeKey.GlobalMatrix, "globalMatrix" }
            };

        // ─────────────────────────────────────────────────────────────
        // Typed IXDocumentAnalyzer
        // ─────────────────────────────────────────────────────────────

        public bool CanHandle(XDocument xDocument)
        {
            return xDocument.Root != null;
        }

        public void Analyze(XDocument xDocument, ParserResult result)
        {
            if (xDocument.Root == null)
                return;

            var step = new ParserStepResult
            {
                StepName = "XML body overview",
                Data = new Dictionary<string, object>()
            };

            AnalyzeGenericNodeCounts(xDocument, result, step);
            AnalyzeAssemblyAndPartStructure(xDocument, result, step);
            BuildSummary(step);

            result.OrderedSteps.Add(step);
        }

        // ─────────────────────────────────────────────────────────────
        // Untyped IDocumentAnalyzer + IAnalyzer<XDocument> bridging
        // ─────────────────────────────────────────────────────────────

        bool IDocumentAnalyzer.CanHandle(object document) =>
            document is XDocument doc && CanHandle(doc);

        void IDocumentAnalyzer.Analyze(object document, ParserResult result)
        {
            if (document is XDocument doc)
                Analyze(doc, result);
        }

        void IAnalyzer<XDocument>.Analyze(XDocument input, ParserResult result) =>
            Analyze(input, result);

        // ─────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────

        private static void AnalyzeGenericNodeCounts(
            XDocument doc,
            ParserResult result,
            ParserStepResult step)
        {
            int totalNodes = 1 + doc.Descendants().Count(); // include root
            step.Data["TotalNodeCount"] = totalNodes;
            result.ParserHints["Xml.TotalNodeCount"] = totalNodes.ToString();

            foreach (var kvp in NodeNameMap)
            {
                var key = kvp.Key;
                var name = kvp.Value;

                int count;
                if (key == PGNodeKey.Root)
                {
                    count = doc.Root!.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                }
                else
                {
                    count = doc.Descendants()
                               .Count(x => x.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
                }

                step.Data[$"Count.{key}"] = count;
                result.ParserHints[$"Xml.Count.{key}"] = count.ToString();
            }
        }

        private static void AnalyzeAssemblyAndPartStructure(
            XDocument doc,
            ParserResult result,
            ParserStepResult step)
        {
            string assemblyName = NodeNameMap[PGNodeKey.Assembly];
            string partName = NodeNameMap[PGNodeKey.Part];

            var assemblies = doc.Descendants()
                                .Where(x => x.Name.LocalName.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                                .ToList();

            var parts = doc.Descendants()
                           .Where(x => x.Name.LocalName.Equals(partName, StringComparison.OrdinalIgnoreCase))
                           .ToList();

            int assemblyCount = assemblies.Count;
            int partCount = parts.Count;

            // "Orphan" Part = Part that has no Assembly ancestor.
            int orphanPartsCount = parts
                .Count(p => !p.Ancestors()
                              .Any(a => a.Name.LocalName.Equals(assemblyName, StringComparison.OrdinalIgnoreCase)));

            step.Data["AssemblyCount"] = assemblyCount;
            step.Data["PartCount"] = partCount;
            step.Data["OrphanPartCount"] = orphanPartsCount;

            result.ParserHints["Xml.AssemblyCount"] = assemblyCount.ToString();
            result.ParserHints["Xml.PartCount"] = partCount.ToString();
            result.ParserHints["Xml.OrphanPartCount"] = orphanPartsCount.ToString();

            if (orphanPartsCount > 0)
            {
                result.Warnings.Add(
                    $"Found {orphanPartsCount} Part node(s) that are not nested under an Assembly node.");
            }
        }

        private static void BuildSummary(ParserStepResult step)
        {
            int total = GetInt(step.Data, "TotalNodeCount");
            int assemblies = GetInt(step.Data, "AssemblyCount");
            int parts = GetInt(step.Data, "PartCount");
            int orphanParts = GetInt(step.Data, "OrphanPartCount");

            step.Summary =
                $"Total nodes: {total}. Assemblies: {assemblies}, Parts: {parts}, " +
                (orphanParts > 0
                    ? $"Orphan Parts: {orphanParts}."
                    : "no orphan Parts detected.");
        }

        private static int GetInt(IDictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out var value))
                return 0;

            if (value is int i) return i;
            if (value is string s && int.TryParse(s, out var parsed)) return parsed;
            return 0;
        }
    }
}
