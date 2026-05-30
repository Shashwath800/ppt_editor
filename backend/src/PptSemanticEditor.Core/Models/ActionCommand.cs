using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models;

/// <summary>
/// Action DSL — AI generates ActionCommands instead of directly rewriting JSON.
/// Includes explainability fields (reason, confidence) for every action.
/// </summary>
public class ActionCommand
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;
    // "rename_node", "add_node", "remove_node", "move_node",
    // "add_edge", "remove_edge", "rewrite_text", "change_style",
    // "add_slide", "remove_slide"

    [JsonPropertyName("slide")]
    public int? Slide { get; set; }

    [JsonPropertyName("target")]
    public string? Target { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; } = 0.8;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    [JsonPropertyName("approved")]
    public bool Approved { get; set; }

    [JsonPropertyName("appliedAt")]
    public string? AppliedAt { get; set; }

    [JsonPropertyName("result")]
    public string? Result { get; set; } // "success", "failed", "skipped"
}
