using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models;

public class PipelineState
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("currentStage")]
    public string CurrentStage { get; set; } = "upload";

    [JsonPropertyName("stages")]
    public Dictionary<string, string> Stages { get; set; } = new()
    {
        ["upload"] = "pending",
        ["openXmlParsing"] = "pending",
        ["astBuilding"] = "pending",
        ["semanticTree"] = "pending",
        ["semanticGraph"] = "pending",
        ["semanticJson"] = "pending",
        ["editPlan"] = "pending",
        ["jsonTransformation"] = "pending",
        ["validation"] = "pending",
        ["rendering"] = "pending",
        ["complete"] = "pending"
    };

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    public void SetStage(string stage, string status)
    {
        if (Stages.ContainsKey(stage))
        {
            Stages[stage] = status;
            CurrentStage = stage;
            UpdatedAt = DateTime.UtcNow.ToString("o");
        }
    }

    public void CompleteStage(string stage)
    {
        SetStage(stage, "completed");
    }

    public void FailStage(string stage)
    {
        SetStage(stage, "error");
    }
}
