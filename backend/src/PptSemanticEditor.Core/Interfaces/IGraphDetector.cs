using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Core.Interfaces;

public class GraphData
{
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphEdge> Edges { get; set; } = new();
}

public class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
}

public class GraphEdge
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public interface IGraphDetector
{
    GraphData? DetectGraph(OpenXmlSlideInfo slide);
}
