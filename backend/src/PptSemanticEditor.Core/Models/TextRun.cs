using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models;

/// <summary>
/// Stores formatting data for a single text run within a paragraph.
/// A run is a contiguous span of text with identical formatting.
/// </summary>
public class TextRun
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("fontSize")]
    public double? FontSize { get; set; }

    [JsonPropertyName("fontColor")]
    public string? FontColor { get; set; }

    [JsonPropertyName("bold")]
    public bool Bold { get; set; }

    [JsonPropertyName("italic")]
    public bool Italic { get; set; }

    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }
}
