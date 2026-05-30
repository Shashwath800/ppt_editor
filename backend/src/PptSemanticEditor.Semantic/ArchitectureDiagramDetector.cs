using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Semantic;

/// <summary>
/// Detects known architecture diagram patterns from a SemanticGraph.
/// Converts raw graphs into typed representations: pipeline, hierarchy, star, layered, etc.
/// </summary>
public class ArchitectureDiagramDetector : IArchitectureDiagramDetector
{
    public ArchitecturePattern? DetectPattern(SemanticGraph graph)
    {
        if (graph.Nodes.Count < 2 || graph.Edges.Count == 0)
            return null;

        // Check each pattern in order of specificity
        var pipeline = DetectPipeline(graph);
        if (pipeline != null) return pipeline;

        var hierarchy = DetectHierarchy(graph);
        if (hierarchy != null) return hierarchy;

        var star = DetectStar(graph);
        if (star != null) return star;

        var layered = DetectLayered(graph);
        if (layered != null) return layered;

        return new ArchitecturePattern
        {
            PatternType = "flowchart",
            Confidence = 0.6,
            Description = "Generic connected graph"
        };
    }

    /// <summary>Pipeline: A → B → C → D (linear chain)</summary>
    private ArchitecturePattern? DetectPipeline(SemanticGraph graph)
    {
        // Build adjacency: each node has at most 1 in-edge and 1 out-edge
        var outDegree = graph.Nodes.ToDictionary(n => n.Id, _ => 0);
        var inDegree = graph.Nodes.ToDictionary(n => n.Id, _ => 0);

        foreach (var edge in graph.Edges)
        {
            if (outDegree.ContainsKey(edge.From)) outDegree[edge.From]++;
            if (inDegree.ContainsKey(edge.To)) inDegree[edge.To]++;
        }

        var maxOut = outDegree.Values.DefaultIfEmpty(0).Max();
        var maxIn = inDegree.Values.DefaultIfEmpty(0).Max();

        // Pure pipeline: all nodes have at most 1 in and 1 out
        if (maxOut <= 1 && maxIn <= 1 && graph.Edges.Count == graph.Nodes.Count - 1)
        {
            return new ArchitecturePattern
            {
                PatternType = "pipeline",
                Confidence = 0.92,
                Description = $"Linear pipeline: {graph.Nodes.Count} stages, {graph.Edges.Count} transitions"
            };
        }

        return null;
    }

    /// <summary>Hierarchy: Tree structure (org chart, taxonomy)</summary>
    private ArchitecturePattern? DetectHierarchy(SemanticGraph graph)
    {
        var inDegree = graph.Nodes.ToDictionary(n => n.Id, _ => 0);
        foreach (var edge in graph.Edges)
            if (inDegree.ContainsKey(edge.To)) inDegree[edge.To]++;

        var roots = inDegree.Where(kv => kv.Value == 0).ToList();
        var leaves = inDegree.Where(kv => kv.Value > 0).ToList();

        if (roots.Count == 1 && graph.Edges.Count == graph.Nodes.Count - 1)
        {
            return new ArchitecturePattern
            {
                PatternType = "hierarchy",
                Confidence = 0.88,
                Description = $"Hierarchy tree: 1 root, {graph.Nodes.Count - 1} children"
            };
        }

        return null;
    }

    /// <summary>Star: One central hub connected to many spokes</summary>
    private ArchitecturePattern? DetectStar(SemanticGraph graph)
    {
        var degree = graph.Nodes.ToDictionary(n => n.Id, _ => 0);
        foreach (var edge in graph.Edges)
        {
            if (degree.ContainsKey(edge.From)) degree[edge.From]++;
            if (degree.ContainsKey(edge.To)) degree[edge.To]++;
        }

        var maxDegree = degree.Values.DefaultIfEmpty(0).Max();
        var hub = degree.FirstOrDefault(kv => kv.Value == maxDegree);

        // Hub connects to > half the nodes
        if (maxDegree >= graph.Nodes.Count / 2 && maxDegree >= 3)
        {
            var hubNode = graph.Nodes.FirstOrDefault(n => n.Id == hub.Key);
            return new ArchitecturePattern
            {
                PatternType = "star",
                Confidence = 0.85,
                Description = $"Star/Hub pattern: '{hubNode?.Label}' connects to {maxDegree} nodes"
            };
        }

        return null;
    }

    /// <summary>Layered: Nodes arranged in horizontal or vertical bands</summary>
    private ArchitecturePattern? DetectLayered(SemanticGraph graph)
    {
        if (graph.Nodes.Count < 3) return null;

        // Cluster nodes by Y position (horizontal layers)
        var yGroups = graph.Nodes
            .GroupBy(n => Math.Round(n.Y, 0))
            .Where(g => g.Count() >= 1)
            .OrderBy(g => g.Key)
            .ToList();

        // At least 2 distinct layers with cross-layer edges
        if (yGroups.Count >= 2)
        {
            var nodeToLayer = new Dictionary<string, int>();
            for (int i = 0; i < yGroups.Count; i++)
                foreach (var node in yGroups[i])
                    nodeToLayer[node.Id] = i;

            var crossLayerEdges = graph.Edges
                .Count(e => nodeToLayer.TryGetValue(e.From, out var l1) &&
                            nodeToLayer.TryGetValue(e.To, out var l2) &&
                            l1 != l2);

            if (crossLayerEdges > 0)
            {
                return new ArchitecturePattern
                {
                    PatternType = "layered",
                    Confidence = 0.80,
                    Description = $"Layered architecture: {yGroups.Count} layers"
                };
            }
        }

        return null;
    }
}
