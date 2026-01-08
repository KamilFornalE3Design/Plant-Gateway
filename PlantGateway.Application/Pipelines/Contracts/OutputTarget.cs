using SMSgroup.Aveva.Config.Models.ValueObjects;
using System;
using System.Collections.Generic;

namespace PlantGateway.Application.Pipelines.Contracts
{
    // -------------------------------------------------------------------- //
    // The hierarchical structure idea that is represented with this contract:
    // OutputTarget
    // └── Paths(URI to file/DB/endpoint)
    //     └── Identifiers(logical element per path)
    //          └── Data(actual attributes/records, defined outside)
    // -------------------------------------------------------------------- //

    /// <summary>
    /// Represents the destination(s) for writer output.
    /// One contract can define multiple paths (files, DBs, APIs),
    /// and within each path, multiple identifiers (tables, elements, sections).
    /// </summary>
    public sealed class OutputTarget
    {
        /// <summary>
        /// Common output format for this pipeline run (Console, File, API, Db).
        /// </summary>
        public OutputSinkType SinkType { get; set; }

        /// <summary>
        /// Collection of output paths, keyed by a logical name.
        /// Example:
        ///   - "File" → local file URI
        ///   - "Db" → database URI/connection
        ///   - "Api" → endpoint URI
        /// </summary>
        public Dictionary<string, OutputPathDefinition> Paths { get; private set; }

        public OutputTarget()
        {
            Paths = new Dictionary<string, OutputPathDefinition>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Add or replace a path definition.</summary>
        public void AddPath(string key, OutputPathDefinition pathDef)
        {
            Paths[key] = pathDef;
        }

        /// <summary>Check if a path is defined.</summary>
        public bool HasPath(string key)
        {
            return Paths.ContainsKey(key);
        }
    }

    /// <summary>
    /// Defines one physical/logical path for outputs.
    /// - File: file URI
    /// - DB: connection string/URI
    /// - API: endpoint URI
    /// Each path can contain multiple identifiers.
    /// </summary>
    public sealed class OutputPathDefinition
    {
        public OutputDataFormat DataFormat { get; set; }

        /// <summary>
        /// Path or URI to the sink:
        /// - File: full path or base directory
        /// - DB: connection string or db:// URI
        /// - API: endpoint URL
        /// </summary>
        public string Uri { get; set; }

        public Dictionary<string, OutputIdentifier> Identifiers { get; private set; }

        public OutputPathDefinition()
        {
            Uri = string.Empty;
            Identifiers = new Dictionary<string, OutputIdentifier>(StringComparer.OrdinalIgnoreCase);
        }

        public void AddIdentifier(string key, OutputIdentifier id)
        {
            Identifiers[key] = id;
        }
    }

    /// <summary>
    /// Logical identifier under a path.
    /// Groups related data (rows, attributes, lines).
    /// </summary>
    public sealed class OutputIdentifier
    {
        /// <summary>
        /// Identifier value, e.g. table name, GUID, element ID, or file suffix.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Optional description for logs/CLI.
        /// </summary>
        public string Description { get; set; }

        public OutputIdentifier()
        {
            Value = string.Empty;
            Description = string.Empty;
        }
    }
}
