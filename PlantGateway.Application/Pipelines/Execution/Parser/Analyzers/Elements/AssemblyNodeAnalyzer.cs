using PlantGateway.Application.Pipelines.Execution.Parser.Interfaces;
using SMSgroup.Aveva.Config.Models.ExecutionResults.Parser;
using SMSgroup.Aveva.Config.Models.ValueObjects;
using SMSgroup.Aveva.Utilities.Parser.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlantGateway.Application.Pipelines.Execution.Parser.Analyzers.Elements
{
    /// <summary>
    /// Surface-level analyzer for Assembly elements.
    /// Parser phase only:
    /// - counts top-level vs nested assemblies
    /// - checks presence of Matrix/globalMatrix on nested assemblies
    /// </summary>
    public sealed class AssemblyNodeAnalyzer : IXElementAnalyzer
    {
        private static readonly string AssemblyName = PGNodeKey.Assembly.ToString();
        private static readonly string RootName = PGNodeKey.Root.ToString();
        private static readonly string MatrixName = PGNodeKey.Matrix.ToString();
        private static readonly string GlobalMatrixName = PGNodeKey.GlobalMatrix.ToString();

        // ─────────────────────────────────────────────────────────────
        // Typed IXElementAnalyzer
        // ─────────────────────────────────────────────────────────────

        public bool CanHandle(XElement xElement) =>
            xElement.Name.LocalName.Equals(AssemblyName, StringComparison.OrdinalIgnoreCase);

        public void Analyze(XElement xElement, ParserResult result)
        {
            // Aggregate counts in ParserHints. Footer/Body analyzers can later
            // build a summary step from these counters if desired.

            IncrementHint(result, "Xml.Assemblies.Total");

            bool isTopLevel = IsDirectChildOf(xElement, RootName);
            if (isTopLevel)
                IncrementHint(result, "Xml.Assemblies.TopLevel");
            else
                IncrementHint(result, "Xml.Assemblies.Nested");

            bool hasMatrix = HasMatrixChild(xElement);
            if (hasMatrix)
            {
                IncrementHint(result, "Xml.Assemblies.WithMatrix");
            }
            else if (!isTopLevel)
            {
                // Only nested Assemblies are expected to have matrices
                IncrementHint(result, "Xml.Assemblies.NestedMissingMatrix");

                result.Warnings.Add(
                    "Nested Assembly element without Matrix/globalMatrix direct child encountered (surface-level check).");
            }

            // Additional Assembly attributes can be checked here later (name, ids, etc.)
        }

        // ─────────────────────────────────────────────────────────────
        // Untyped IElementAnalyzer + IAnalyzer<XElement> bridging
        // ─────────────────────────────────────────────────────────────

        bool IElementAnalyzer.CanHandle(object element) =>
            element is XElement el && CanHandle(el);

        void IElementAnalyzer.Analyze(object element, ParserResult result)
        {
            if (element is XElement el)
                Analyze(el, result);
        }

        void IAnalyzer<XElement>.Analyze(XElement input, ParserResult result) =>
            Analyze(input, result);

        // ─────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────

        private static bool IsDirectChildOf(XElement element, string parentLocalName) =>
            element.Parent != null &&
            element.Parent.Name.LocalName.Equals(parentLocalName, StringComparison.OrdinalIgnoreCase);

        private static bool HasMatrixChild(XElement element)
        {
            foreach (var child in element.Elements())
            {
                var local = child.Name.LocalName;
                if (local.Equals(MatrixName, StringComparison.OrdinalIgnoreCase) ||
                    local.Equals(GlobalMatrixName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void IncrementHint(ParserResult result, string key)
        {
            if (!result.ParserHints.TryGetValue(key, out var existing) || existing is null)
            {
                // First time we see this key → start at 1
                result.ParserHints[key] = 1;
                return;
            }

            int value;

            switch (existing)
            {
                case int i:
                    value = i;
                    break;

                case string s when int.TryParse(s, out var parsed):
                    value = parsed;
                    break;

                default:
                    // Anything else (double, bool, etc.) – treat as 0 and start counting
                    value = 0;
                    break;
            }

            result.ParserHints[key] = value + 1;
        }
    }
}
