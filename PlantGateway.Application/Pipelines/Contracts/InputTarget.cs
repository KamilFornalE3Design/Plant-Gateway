using SMSgroup.Aveva.Config.Models.ValueObjects;
using System;
using System.IO;

namespace PlantGateway.Application.Pipelines.Contracts
{
    /// <summary>
    /// Represents a unified input contract for walkers in the conversion pipeline.
    /// Encapsulates both the source of data (file, DB, etc.) and the resolved format.
    /// </summary>
    /// <remarks>
    /// This class is passed into walkers (<see cref="IWalkerStrategy{TDto}"/>)
    /// to provide a consistent definition of the input source.
    /// It allows the CLI and factories to build a single object that describes:
    /// <list type="bullet">
    /// <item><description>The origin (file path, DB connection string, schema)</description></item>
    /// <item><description>The format (TXT, XML, JSON, DB)</description></item>
    /// </list>
    /// </remarks>
    public sealed class InputTarget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputTarget"/> class
        /// using the specified file path. The <see cref="Format"/> property
        /// is automatically resolved from the file extension.
        /// </summary>
        /// <param name="fullPath">The full path to the input file.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="fullPath"/> is null, empty, or whitespace.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown when the file extension is not mapped to a supported <see cref="InputDataFormat"/>.
        /// </exception>

        public InputTarget()
        {

        }

        /// <summary>
        /// Gets or sets the resolved input format (TXT, XML, JSON, DB).
        /// </summary>
        public InputDataFormat Format { get; set; }

        /// <summary>
        /// Gets or sets the full file path for file-based walkers.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Version/Revison related to Input Target.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the connection string for DB-based walkers.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional schema, root node, or dataset name.
        /// Used primarily for DB or hierarchical file formats.
        /// </summary>
        public string Schema { get; set; } = string.Empty;

        /// <summary>
        /// Resolves the <see cref="InputDataFormat"/> based on the file extension.
        /// </summary>
        /// <param name="filePath">The file path to evaluate.</param>
        /// <returns>The detected <see cref="InputDataFormat"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> is null or empty.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown when the file extension does not match any supported format.
        /// </exception>
        private InputDataFormat ResolveInputFormat(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            switch (ext)
            {
                case ".xml":
                    return InputDataFormat.xml;
                case ".txt":
                    return InputDataFormat.txt;
                case ".csv":
                    return InputDataFormat.csv;
                case ".json":
                    return InputDataFormat.json;
                case ".db":
                case ".sqlite":
                    return InputDataFormat.db;
                default:
                    throw new NotSupportedException($"Unsupported input file extension: {ext}");
            }
        }
    }
}
