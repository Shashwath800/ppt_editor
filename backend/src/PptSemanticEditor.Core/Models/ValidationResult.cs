using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models;

/// <summary>
/// Result of running the validation engine before rendering.
/// </summary>
public class ValidationResult
{
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("errors")]
    public List<ValidationError> Errors { get; set; } = new();

    [JsonPropertyName("warnings")]
    public List<ValidationWarning> Warnings { get; set; } = new();

    [JsonPropertyName("validatedAt")]
    public string ValidatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("summary")]
    public string Summary => IsValid
        ? $"Validation passed with {Warnings.Count} warning(s)"
        : $"Validation failed: {Errors.Count} error(s), {Warnings.Count} warning(s)";
}

public class ValidationError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    // e.g. "ORPHAN_NODE", "INVALID_EDGE", "DUPLICATE_ID", "MISSING_TITLE"

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;
    // element ID, slide ID, or "presentation"

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "error";
    // "error" (blocks render), "critical" (data loss risk)

    [JsonPropertyName("slide")]
    public int? Slide { get; set; }
}

public class ValidationWarning
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    // e.g. "OVERLAPPING_ELEMENTS", "LOW_CONFIDENCE_EDGE", "MISSING_ALT_TEXT"

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("slide")]
    public int? Slide { get; set; }
}
