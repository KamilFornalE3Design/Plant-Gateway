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

namespace SMSgroup.Aveva.Utilities.Parser.Analyzers.Document.Xml
{
    /// <summary>
    /// Document-level analyzer for the header section:
    /// &lt;Root&gt;&lt;Header&gt;&lt;Info/&gt;&lt;Export/&gt;&lt;Units/&gt;&lt;/Header&gt;.
    /// Surface-level only: extracts basic metadata and guesses source system.
    /// </summary>
    public sealed class HeaderAnalyzer : IXDocumentAnalyzer
    {
        // Small map to guess source system from free-form strings.
        private static readonly IReadOnlyDictionary<string, SourceSystem> _map = new Dictionary<string, SourceSystem>(StringComparer.OrdinalIgnoreCase)
            {
                { "creo", SourceSystem.CreoParametric },
                { "ptc creo", SourceSystem.CreoParametric },
                { "ptc creo parametric", SourceSystem.CreoParametric },
                { "creo parametric", SourceSystem.CreoParametric },
                { "inventor", SourceSystem.Inventor },
                { "autodesk inventor", SourceSystem.Inventor },
            };

        // ─────────────────────────────────────────────────────────────
        // Typed IXDocumentAnalyzer
        // ─────────────────────────────────────────────────────────────

        public bool CanHandle(XDocument xDocument)
        {
            var root = xDocument.Root;
            return root != null; // Any Root doc can be checked for Header.
        }

        public void Analyze(XDocument xDocument, ParserResult result)
        {
            var root = xDocument.Root;
            if (root == null)
                return;

            var header = root.Element("Header");
            if (header == null)
            {
                result.Warnings.Add("Missing Header section.");
                return;
            }

            var info = header.Element("Info");
            var export = header.Element("Export");
            var units = header.Element("Units");

            // Build a step for diagnostics UI
            var step = new ParserStepResult
            {
                StepName = "Header analysis",
                Data = new Dictionary<string, object>()
            };

            AnalyzeInfo(info, result, step);
            AnalyzeExport(export, result, step);
            AnalyzeUnits(units, result, step);
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

        private static void AnalyzeInfo(
            XElement? info,
            ParserResult result,
            ParserStepResult step)
        {
            if (info == null)
            {
                step.Data["HasInfo"] = false;
                result.ParserHints["Header.HasInfo"] = "false";
                return;
            }

            step.Data["HasInfo"] = true;
            result.ParserHints["Header.HasInfo"] = "true";

            // Copy all attributes as hints with prefix
            foreach (var attr in info.Attributes())
            {
                var key = $"Header.Info.{attr.Name.LocalName}";
                result.ParserHints[key] = attr.Value;
                step.Data[key] = attr.Value;
            }

            // Try to guess source system from common attribute names
            var rawSource = info.Attribute("source")?.Value
                            ?? info.Attribute("Source")?.Value
                            ?? info.Attribute("System")?.Value
                            ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(rawSource))
            {
                var src = GuessSourceSystem(rawSource);
                result.ParserHints["Header.SourceRaw"] = rawSource;
                result.ParserHints["Header.SourceSystem"] = src.ToString();

                step.Data["Header.SourceRaw"] = rawSource;
                step.Data["Header.SourceSystem"] = src.ToString();
            }
        }

        private static void AnalyzeExport(
            XElement? export,
            ParserResult result,
            ParserStepResult step)
        {
            if (export == null)
            {
                step.Data["HasExport"] = false;
                result.ParserHints["Header.HasExport"] = "false";
                return;
            }

            step.Data["HasExport"] = true;
            result.ParserHints["Header.HasExport"] = "true";

            foreach (var attr in export.Attributes())
            {
                var key = $"Header.Export.{attr.Name.LocalName}";
                result.ParserHints[key] = attr.Value;
                step.Data[key] = attr.Value;
            }
        }

        private static void AnalyzeUnits(
            XElement? units,
            ParserResult result,
            ParserStepResult step)
        {
            if (units == null)
            {
                step.Data["HasUnits"] = false;
                result.ParserHints["Header.HasUnits"] = "false";
                return;
            }

            step.Data["HasUnits"] = true;
            result.ParserHints["Header.HasUnits"] = "true";

            foreach (var attr in units.Attributes())
            {
                var key = $"Header.Units.{attr.Name.LocalName}";
                result.ParserHints[key] = attr.Value;
                step.Data[key] = attr.Value;
            }
        }

        private static void BuildSummary(ParserStepResult step)
        {
            bool hasInfo = GetBool(step.Data, "HasInfo");
            bool hasExport = GetBool(step.Data, "HasExport");
            bool hasUnits = GetBool(step.Data, "HasUnits");

            step.Summary =
                $"Header present: Info={hasInfo}, Export={hasExport}, Units={hasUnits}.";
        }

        private static bool GetBool(IDictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out var value))
                return false;

            if (value is bool b) return b;
            if (value is string s && bool.TryParse(s, out var parsed)) return parsed;
            return false;
        }

        private static SourceSystem GuessSourceSystem(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return SourceSystem.Unknown;

            var normalized = input.Trim();

            foreach (var kvp in _map)
            {
                if (normalized.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return kvp.Value;
            }

            return SourceSystem.Unknown;
        }
    }
}
