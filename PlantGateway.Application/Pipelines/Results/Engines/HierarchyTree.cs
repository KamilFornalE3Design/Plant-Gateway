using System.Collections.Generic;

namespace PlantGateway.Application.Pipelines.Results.Engines
{
    public sealed class HierarchyTree
    {
        public List<HierarchyNode> Roots { get; set; } = new List<HierarchyNode>();
    }
}
