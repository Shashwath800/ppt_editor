using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models;

/// <summary>
/// Tracks all versions of the semantic representation per session.
/// Supports rollback, diff generation, and audit history.
/// </summary>
public class SemanticDocumentVersion
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    // "Original", "AI Edit: rewrite titles", "Manual edit", etc.

    [JsonPropertyName("snapshot")]
    public SemanticPresentation Snapshot { get; set; } = new();

    [JsonPropertyName("appliedActions")]
    public List<ActionCommand>? AppliedActions { get; set; }

    [JsonPropertyName("changedSlides")]
    public List<int> ChangedSlides { get; set; } = new();
}

public class VersionHistory
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("versions")]
    public List<SemanticDocumentVersion> Versions { get; set; } = new();

    [JsonPropertyName("currentVersion")]
    public int CurrentVersion => Versions.Count > 0 ? Versions.Max(v => v.Version) : 0;

    public void AddVersion(SemanticPresentation snapshot, string description, List<ActionCommand>? actions = null)
    {
        var version = new SemanticDocumentVersion
        {
            Version = CurrentVersion + 1,
            Timestamp = DateTime.UtcNow.ToString("o"),
            Description = description,
            Snapshot = snapshot,
            AppliedActions = actions
        };
        Versions.Add(version);
    }

    public SemanticDocumentVersion? GetVersion(int version)
        => Versions.FirstOrDefault(v => v.Version == version);

    public SemanticDocumentVersion? GetLatest()
        => Versions.OrderByDescending(v => v.Version).FirstOrDefault();
}
