using System.Text.Json;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Semantic;

public class JsonGenerator
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public string Serialize(SemanticPresentation presentation)
    {
        return JsonSerializer.Serialize(presentation, SerializerOptions);
    }

    public SemanticPresentation? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<SemanticPresentation>(json, SerializerOptions);
    }

    public static JsonSerializerOptions GetOptions() => SerializerOptions;
}
