using System;

namespace PlantGateway.Core.Config.Attributes
{
    /// <summary>
    /// Marks a DTO property as an enriched field — 
    /// data added or resolved during processing, enrichment, or pipeline stages.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class EnrichedInputAttribute : Attribute { }
}
