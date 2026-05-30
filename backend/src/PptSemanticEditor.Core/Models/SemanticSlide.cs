using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models;

public class SemanticSlide
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("classification")]
    public string Classification { get; set; } = string.Empty;

    [JsonPropertyName("classificationType")]
    public string ClassificationType { get; set; } = string.Empty;

    [JsonPropertyName("elements")]
    public List<SemanticElement> Elements { get; set; } = new();

    [JsonPropertyName("relationships")]
    public List<SemanticRelationship> Relationships { get; set; } = new();

    [JsonPropertyName("graph")]
    public SemanticGraph? Graph { get; set; }
}
