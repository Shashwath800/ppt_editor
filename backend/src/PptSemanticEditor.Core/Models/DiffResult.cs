using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models;

public class DiffResult
{
    [JsonPropertyName("original")]
    public SemanticPresentation Original { get; set; } = new();

    [JsonPropertyName("modified")]
    public SemanticPresentation Modified { get; set; } = new();
}
