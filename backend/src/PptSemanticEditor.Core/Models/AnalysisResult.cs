using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models;

public class AnalysisResult
{
    [JsonPropertyName("analysis")]
    public List<string> Analysis { get; set; } = new();

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}
