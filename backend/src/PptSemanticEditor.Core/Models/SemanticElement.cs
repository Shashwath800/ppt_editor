using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models;

public class SemanticElement
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("fontSize")]
    public double FontSize { get; set; }

    [JsonPropertyName("fontColor")]
    public string FontColor { get; set; } = string.Empty;

    [JsonPropertyName("fillColor")]
    public string FillColor { get; set; } = string.Empty;

    [JsonPropertyName("imageBase64")]
    public string? ImageBase64 { get; set; }

    [JsonPropertyName("rotation")]
    public double Rotation { get; set; }

    [JsonPropertyName("zIndex")]
    public int ZIndex { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>
    /// Rich paragraph formatting data extracted from the original PPTX.
    /// Used as a template when rendering edited text back to PPTX,
    /// so that per-paragraph colors, numbering, and bullets are preserved.
    /// The flat Text property remains the primary data the AI works with.
    /// </summary>
    [JsonPropertyName("paragraphs")]
    public List<TextParagraph>? Paragraphs { get; set; }
}
