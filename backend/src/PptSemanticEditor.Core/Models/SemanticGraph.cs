using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models;

/// <summary>
/// First-class semantic graph representation for slides containing
/// flowcharts, architecture diagrams, org charts, pipelines, etc.
/// </summary>
public class SemanticGraph
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [JsonPropertyName("graphType")]
    public string GraphType { get; set; } = "unknown";
    // "pipeline", "flowchart", "hierarchy", "architecture", "dependency", "star", "layered"

    [JsonPropertyName("flowDirection")]
    public string FlowDirection { get; set; } = "unknown";
    // "left_to_right", "top_to_bottom", "right_to_left", "bottom_to_top", "radial"

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; } = 1.0;

    [JsonPropertyName("nodes")]
    public List<SemanticGraphNode> Nodes { get; set; } = new();

    [JsonPropertyName("edges")]
    public List<SemanticGraphEdge> Edges { get; set; } = new();
}

public class SemanticGraphNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("nodeType")]
    public string NodeType { get; set; } = "process";
    // "process", "decision", "terminal", "data", "service", "database", "cloud"

    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();
}

public class SemanticGraphEdge
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("edgeType")]
    public string EdgeType { get; set; } = "flow";
    // "flow", "dependency", "data_flow", "hierarchy", "inferred"

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; } = 1.0;
    // 1.0 = explicit connector, <1.0 = geometry-inferred
}
