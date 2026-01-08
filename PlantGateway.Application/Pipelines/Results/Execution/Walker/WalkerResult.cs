using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Application.Pipelines.Results.Execution
{
    public sealed class WalkerResult<TDto>
    {
        public bool IsSuccess { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public List<string> RawLines { get; set; } = new List<string>();
        public List<Dictionary<string, string>> MappedRows { get; set; } = new List<Dictionary<string, string>>();
        public List<TDto> Dtos { get; set; } = new List<TDto>();
        public List<string> Warnings { get; set; } = new List<string>();
        public Dictionary<string, object> ParserHints { get; set; } = new Dictionary<string, object>();
        public Dictionary<WalkerModificationType, List<string>> Modifications { get; set; } = new Dictionary<WalkerModificationType, List<string>>();
    }
    /// <summary>
    /// Defines categories of data modifications performed during the Walker stage.
    /// </summary>
    public enum WalkerModificationType
    {
        Unknown = 0,
        Merge = 1,
        Normalization = 2,
        Correction = 3,
        Replacement = 4,
        Removal = 5
    }
}
