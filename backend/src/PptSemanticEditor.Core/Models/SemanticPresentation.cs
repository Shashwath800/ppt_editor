using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models;

public class SemanticPresentation
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("slideCount")]
    public int SlideCount { get; set; }

    [JsonPropertyName("slideWidth")]
    public double SlideWidth { get; set; }

    [JsonPropertyName("slideHeight")]
    public double SlideHeight { get; set; }

    [JsonPropertyName("slides")]
    public List<SemanticSlide> Slides { get; set; } = new();
}
