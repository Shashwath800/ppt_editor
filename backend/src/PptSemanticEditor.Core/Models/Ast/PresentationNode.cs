using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models.Ast;

/// <summary>
/// Abstract base for all AST nodes — mirrors a compiler AST node hierarchy.
/// </summary>
public abstract class PresentationNode
{
    [JsonPropertyName("nodeId")]
    public string NodeId { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [JsonPropertyName("nodeType")]
    public abstract string NodeType { get; }

    [JsonPropertyName("children")]
    public List<PresentationNode> Children { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class PresentationRootNode : PresentationNode
{
    [JsonPropertyName("nodeType")]
    public override string NodeType => "presentation";

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("slideWidth")]
    public double SlideWidth { get; set; }

    [JsonPropertyName("slideHeight")]
    public double SlideHeight { get; set; }

    [JsonPropertyName("slideCount")]
    public int SlideCount { get; set; }
}

public class SlideNode : PresentationNode
{
    [JsonPropertyName("nodeType")]
    public override string NodeType => "slide";

    [JsonPropertyName("slideIndex")]
    public int SlideIndex { get; set; }

    [JsonPropertyName("slideTitle")]
    public string SlideTitle { get; set; } = string.Empty;

    [JsonPropertyName("classification")]
    public string Classification { get; set; } = string.Empty;
}

public class ShapeNode : PresentationNode
{
    [JsonPropertyName("nodeType")]
    public override string NodeType => "shape";

    [JsonPropertyName("shapeId")]
    public string ShapeId { get; set; } = string.Empty;

    [JsonPropertyName("shapeKind")]
    public string ShapeKind { get; set; } = string.Empty; // "rectangle", "ellipse", etc.

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("rotation")]
    public double Rotation { get; set; }

    [JsonPropertyName("zIndex")]
    public int ZIndex { get; set; }

    [JsonPropertyName("fillColor")]
    public string FillColor { get; set; } = string.Empty;
}

public class TextNode : PresentationNode
{
    [JsonPropertyName("nodeType")]
    public override string NodeType => "text";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("fontSize")]
    public double FontSize { get; set; }

    [JsonPropertyName("fontColor")]
    public string FontColor { get; set; } = string.Empty;

    [JsonPropertyName("isBold")]
    public bool IsBold { get; set; }

    [JsonPropertyName("isTitle")]
    public bool IsTitle { get; set; }
}

public class ImageNode : PresentationNode
{
    [JsonPropertyName("nodeType")]
    public override string NodeType => "image";

    [JsonPropertyName("imageBase64")]
    public string? ImageBase64 { get; set; }

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;
}

public class ConnectorNode : PresentationNode
{
    [JsonPropertyName("nodeType")]
    public override string NodeType => "connector";

    [JsonPropertyName("fromNodeId")]
    public string? FromNodeId { get; set; }

    [JsonPropertyName("toNodeId")]
    public string? ToNodeId { get; set; }

    [JsonPropertyName("connectorType")]
    public string ConnectorType { get; set; } = "straight"; // "straight", "elbow", "curved"
}

public class GraphAstNode : PresentationNode
{
    [JsonPropertyName("nodeType")]
    public override string NodeType => "graph";

    [JsonPropertyName("graphType")]
    public string GraphType { get; set; } = string.Empty;

    [JsonPropertyName("nodeCount")]
    public int NodeCount { get; set; }

    [JsonPropertyName("edgeCount")]
    public int EdgeCount { get; set; }
}
