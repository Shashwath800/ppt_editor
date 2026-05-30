using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models;

public class OpenXmlInfo
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("slides")]
    public List<OpenXmlSlideInfo> Slides { get; set; } = new();

    [JsonPropertyName("slideWidth")]
    public double SlideWidth { get; set; }

    [JsonPropertyName("slideHeight")]
    public double SlideHeight { get; set; }
}

public class OpenXmlSlideInfo
{
    [JsonPropertyName("slideIndex")]
    public int SlideIndex { get; set; }

    [JsonPropertyName("rawXml")]
    public string RawXml { get; set; } = string.Empty;

    [JsonPropertyName("shapes")]
    public List<OpenXmlShapeInfo> Shapes { get; set; } = new();

    [JsonPropertyName("relationships")]
    public List<OpenXmlRelationshipInfo> Relationships { get; set; } = new();
}

public class OpenXmlShapeInfo
{
    [JsonPropertyName("shapeId")]
    public string ShapeId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("rawXml")]
    public string RawXml { get; set; } = string.Empty;

    // EMU-based position and size (used internally)
    public long OffsetX { get; set; }
    public long OffsetY { get; set; }
    public long ExtentCx { get; set; }
    public long ExtentCy { get; set; }

    // Extracted styling
    public double FontSize { get; set; }
    public string FontColor { get; set; } = string.Empty;
    public string FillColor { get; set; } = string.Empty;
    public string? ImageBase64 { get; set; }
    public bool IsTitle { get; set; }
    public bool IsConnector { get; set; }
    public string? ConnectorStartId { get; set; }
    public string? ConnectorEndId { get; set; }
}

public class OpenXmlRelationshipInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;
}
