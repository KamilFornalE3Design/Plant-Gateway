using System;

namespace PlantGateway.Core.Config.Models.Maps
{
    /// <summary>
    /// Data Transfer Object (DTO) representing metadata and diagnostics for a file.
    /// </summary>
    public sealed class FileInfoDTO
    {
        /// <summary>
        /// Full absolute path to the file.
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// File name with extension.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Parent directory of the file.
        /// </summary>
        public string DirectoryPath { get; set; } = string.Empty;

        /// <summary>
        /// File extension including the leading dot (e.g. ".xml").
        /// </summary>
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// True if the file exists on disk at the given path.
        /// </summary>
        public bool Exists { get; set; }

        /// <summary>
        /// True if the file is marked as read-only.
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// True if the file can be written (not read-only and write permissions are available).
        /// </summary>
        public bool IsWritable { get; set; }

        /// <summary>
        /// Size of the file in bytes.
        /// </summary>
        public long SizeInBytes { get; set; }

        /// <summary>
        /// Number of lines in the file (if text-based).
        /// </summary>
        public long LineCount { get; set; }

        /// <summary>
        /// Creation time (UTC).
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Last modified time (UTC).
        /// </summary>
        public DateTime ModifiedOn { get; set; }

        /// <summary>
        /// Last accessed time (UTC).
        /// </summary>
        public DateTime AccessedOn { get; set; }

        // ---------------------------
        // Conversion and Validation
        // ---------------------------

        /// <summary>
        /// Returns file size in kilobytes (KB).
        /// </summary>
        public double SizeInKB() => Math.Round(SizeInBytes / 1024.0, 2);

        /// <summary>
        /// Returns file size in megabytes (MB).
        /// </summary>
        public double SizeInMB() => Math.Round(SizeInBytes / 1024.0 / 1024.0, 2);

        /// <summary>
        /// Returns file size in gigabytes (GB).
        /// </summary>
        public double SizeInGB() => Math.Round(SizeInBytes / 1024.0 / 1024.0 / 1024.0, 2);

        /// <summary>
        /// Validates if the file has the expected extension (case-insensitive).
        /// </summary>
        public bool ValidateExtension(string expectedExtension)
        {
            if (string.IsNullOrWhiteSpace(expectedExtension))
                return false;

            return string.Equals(Extension, expectedExtension.StartsWith(".") ? expectedExtension : "." + expectedExtension,
                                 StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Human-readable summary of file info.
        /// </summary>
        public override string ToString()
        {
            return $"{FileName} | {SizeInMB()} MB | Lines: {LineCount} | Exists: {Exists} | Writable: {IsWritable}";
        }
    }
}
