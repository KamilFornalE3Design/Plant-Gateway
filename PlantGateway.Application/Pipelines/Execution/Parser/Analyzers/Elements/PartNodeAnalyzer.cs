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
    /// Surface-level analyzer for Part elements.
    /// Parser phase only:
    /// - Matrix/globalMatrix direct children
    /// - presence of geom / name / AvevaTag attributes
    /// </summary>
    public sealed class PartNodeAnalyzer : IXElementAnalyzer
    {
        private static readonly string PartName = PGNodeKey.Part.ToString();
        private static readonly string MatrixName = PGNodeKey.Matrix.ToString();
        private static readonly string GlobalMatrixName = PGNodeKey.GlobalMatrix.ToString();

        // ─────────────────────────────────────────────────────────────
        // Typed IXElementAnalyzer
        // ─────────────────────────────────────────────────────────────

        public bool CanHandle(XElement xElement) =>
            xElement.Name.LocalName.Equals(PartName, StringComparison.OrdinalIgnoreCase);

        public void Analyze(XElement xElement, ParserResult result)
        {
            IncrementHint(result, "Xml.Parts.Total");

            bool hasMatrix = HasMatrixChild(xElement);
            IncrementHint(result, hasMatrix ? "Xml.Parts.WithMatrix" : "Xml.Parts.WithoutMatrix");

            AnalyzeAttributes(xElement, result);
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

        private static void AnalyzeAttributes(XElement part, ParserResult result)
        {
            var geomAttr = part.Attribute("geom");
            var nameAttr = part.Attribute("name");
            var tagAttr = part.Attribute("AvevaTag") ?? part.Attribute("avevaTag");

            if (geomAttr != null)
                IncrementHint(result, "Xml.Parts.WithGeom");
            else
            {
                IncrementHint(result, "Xml.Parts.WithoutGeom");
                result.Warnings.Add("Part element without 'geom' attribute encountered (surface-level check).");
            }

            if (nameAttr != null)
                IncrementHint(result, "Xml.Parts.WithName");
            else
                IncrementHint(result, "Xml.Parts.WithoutName");

            if (tagAttr != null)
                IncrementHint(result, "Xml.Parts.WithAvevaTag");
            else
                IncrementHint(result, "Xml.Parts.WithoutAvevaTag");
        }

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
