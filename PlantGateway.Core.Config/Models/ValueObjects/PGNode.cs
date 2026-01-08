using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Core.Config.Models.ValueObjects
{
    /// <summary>
    /// Represents a logical Plant Gateway node (Assembly, Part, etc.)
    /// and its optional transformation data.
    /// Generic over the node type (XElement, EF entity, DTO, etc.).
    /// </summary>
    public sealed class PGNode<T> : IPGNode
    {
        public PGNodeKey Type { get; set; }
        public Guid Identifier { get; set; }
        public T Node { get; set; }
        public T Matrix { get; set; } = default(T);
        public T GlobalMatrix { get; set; } = default(T);
        public T AbsoluteMatrix { get; set; } = default(T);
    }
    /// <summary>
    /// Shared contract for all Plant Gateway node representations (XML, DB, DTO).
    /// </summary>
    public interface IPGNode
    {
        PGNodeKey Type { get; }
        Guid Identifier { get; }
    }
}
