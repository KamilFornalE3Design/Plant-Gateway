using System;

namespace PlantGateway.Core.Config.Attributes
{
    /// <summary>
    /// Marks a DTO property as a calculated field — 
    /// data derived mathematically or geometrically (e.g., matrix transforms, computed positions).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CalculatedFieldAttribute : Attribute { }
}
