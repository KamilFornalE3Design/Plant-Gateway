using Serilog;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.EngineResults;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace PlantGateway.Core.Config.Implementations.Maps
{
    /// <summary>
    /// Central service that collects and writes all pipeline results to the Serilog log file.
    /// Encapsulates <see cref="PipelineResult"/> and provides controlled flush to disk.
    /// </summary>
    public sealed class PipelineResultService
    {
        private readonly ILogger _logger;
        private readonly PipelineResult _result;

        public PipelineResultService(ILogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger;
            _result = new PipelineResult();
        }
        public void Flush<TDto>(PipelineResult pipelineResult, PipelineContract<TDto> contract)
        {
            // Fail fast
            if (contract?.Items == null)
                return;

            // Build XML diagnostic
            var xml = BuildEngineResultsXml(contract);

            // Get output path from InputTarget
            var inputPath = contract.Input.FilePath;
            if (string.IsNullOrWhiteSpace(inputPath))
                return;

            // Get directory
            var directory = Path.GetDirectoryName(inputPath);
            // Fallback to current directory of app - bad idea maybe to Local appsettings? It needs definition so the files are not everywhere
            if (string.IsNullOrWhiteSpace(directory))
                directory = Environment.CurrentDirectory;

            // desired name is drop.xml
            var outputPath = Path.Combine(directory, "drop.xml");

            xml.Save(outputPath);
        }

        private XDocument BuildEngineResultsXml<TDto>(PipelineContract<TDto> contract)
        {
            if (contract == null || contract.Items == null)
                throw new ArgumentNullException(nameof(contract), "Contract or items cannot be null.");

            // Root XML element
            var root = new XElement("PipelineExport",
                new XAttribute("Timestamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")));

            // Build DTO → EngineResults structure
            var dtoElements = contract.Items
                .Where(dto => dto != null)
                .Select(dto =>
                {
                    var dtoElem = new XElement("DTO",
                        new XAttribute("Type", dto.GetType().Name));

                    // Add optional DTO identifiers
                    var idProp = dto.GetType().GetProperty("Id");
                    if (idProp != null)
                    {
                        var idVal = idProp.GetValue(dto)?.ToString();
                        if (!string.IsNullOrWhiteSpace(idVal))
                            dtoElem.Add(new XAttribute("Id", idVal));
                    }

                    // Add source file info if present
                    var fileProp = dto.GetType().GetProperty("FileFullPath");
                    if (fileProp != null)
                    {
                        var pathVal = fileProp.GetValue(dto)?.ToString();
                        if (!string.IsNullOrWhiteSpace(pathVal))
                            dtoElem.Add(new XAttribute("SourceFile", pathVal));
                    }

                    // Add engine results
                    var results = ((IPlantGatewayDTO)dto).EngineResults ?? new List<IEngineResult>();
                    foreach (var result in results)
                    {
                        var resultElem = new XElement("EngineResult",
                            new XAttribute("Type", result.GetType().Name));

                        // Dump all public instance properties
                        var props = result.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var prop in props)
                        {
                            object value = null;
                            try { value = prop.GetValue(result, null); } catch { }

                            string formatted;
                            if (value == null)
                                formatted = "(null)";
                            else if (value is IDictionary)
                            {
                                var dict = (IDictionary)value;
                                var parts = new List<string>();
                                foreach (DictionaryEntry e in dict)
                                    parts.Add(e.Key + ":" + e.Value);
                                formatted = "{" + string.Join(", ", parts) + "}";
                            }
                            else if (value is IEnumerable && !(value is string))
                            {
                                var list = new List<string>();
                                foreach (var item in (IEnumerable)value)
                                    list.Add(item != null ? item.ToString() : "(null)");
                                formatted = "[" + string.Join(", ", list) + "]";
                            }
                            else
                            {
                                formatted = value.ToString();
                            }

                            // Add each property as <Property Name="" Value=""/>
                            resultElem.Add(new XElement("Property",
                                new XAttribute("Name", prop.Name),
                                new XAttribute("Value", formatted)));
                        }

                        dtoElem.Add(resultElem);
                    }

                    return dtoElem;
                });

            // Attach DTO elements to root
            root.Add(dtoElements);

            return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
        }
    }
}
