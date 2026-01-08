using System;

namespace PlantGateway.Core.Config.Attributes
{
    /// <summary>
    /// Marks a DTO property as a raw input field — 
    /// data parsed directly from external sources (XML, TXT, CSV, JSON, etc.).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RawInputAttribute : Attribute { }
}
