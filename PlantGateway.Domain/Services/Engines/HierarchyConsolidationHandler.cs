using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Models.EngineResults;

namespace SMSgroup.Aveva.Utilities.Engines
{
    /// <summary>
    /// Consolidates multiple HierarchyEngineResults into a single unified project hierarchy tree.
    /// Merges identical logical nodes (same AvevaType + AvevaTag), resolves parent relationships,
    /// and keeps virtual/real node consistency.
    /// </summary>
    public sealed class HierarchyConsolidationHandler : IEngine
    {
        public HierarchyTree Consolidate(IEnumerable<HierarchyEngineResult> hierarchyResults)
        {
            if (hierarchyResults == null)
                throw new ArgumentNullException(nameof(hierarchyResults));

            // === 1️⃣ Flatten all hierarchy nodes from all DTOs ===
            var allNodes = hierarchyResults
                .SelectMany(r => r.HierarchyChain)
                .GroupBy(n => n.AvevaTag, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var representative = g.First();

                    // ✅ If ANY of the nodes is real (IsVirtual == false), mark the consolidated as real
                    bool hasRealNode = g.Any(n => !n.IsVirtual);
                    bool isVirtual = !hasRealNode;

                    // ✅ If there is a real node, take its Id, otherwise Guid.Empty
                    var realNode = g.FirstOrDefault(n => !n.IsVirtual);
                    var id = realNode?.Id ?? Guid.Empty;

                    return new HierarchyNode
                    {
                        Id = id,
                        AvevaType = representative.AvevaType,
                        AvevaTag = representative.AvevaTag,
                        ParentAvevaTag = representative.ParentAvevaTag,
                        Depth = g.Min(n => n.Depth),
                        IsVirtual = isVirtual,
                        Children = new List<HierarchyNode>()
                    };
                })
                .OrderBy(n => n.Depth)
                .ToList();

            // === 2️⃣ Build parent-child relationships ===
            var lookup = allNodes.ToLookup(n => n.ParentAvevaTag ?? string.Empty);
            foreach (var node in allNodes)
            {
                node.Children = lookup[node.AvevaTag].ToList();
            }

            // === 3️⃣ Identify roots (no parent or empty string)
            var roots = lookup[string.Empty].ToList();

            // === 4️⃣ Return HierarchyTree (for structured writers)
            return new HierarchyTree { Roots = roots };
        }

        /// <summary>
        /// Converts a flat node list into a tree representation.
        /// </summary>
        public HierarchyTree BuildTree(IEnumerable<HierarchyNode> nodes)
        {
            var lookup = nodes.ToLookup(n => n.ParentAvevaTag ?? string.Empty);
            var roots = lookup[string.Empty].ToList();

            foreach (var root in roots)
                BuildChildren(root, lookup);

            return new HierarchyTree { Roots = roots };
        }

        private void BuildChildren(HierarchyNode parent, ILookup<string, HierarchyNode> lookup)
        {
            var children = lookup[parent.AvevaTag].ToList();
            foreach (var child in children)
                BuildChildren(child, lookup);

            parent.Children = children;
        }
    }

}
