using System.Text.Json.Serialization;

namespace PptSemanticEditor.Core.Models;

/// <summary>
/// Stores formatting data for a single paragraph within a text body.
/// Each paragraph contains one or more TextRuns and paragraph-level properties
/// like numbering, bullets, and alignment.
/// </summary>
public class TextParagraph
{
    [JsonPropertyName("runs")]
    public List<TextRun> Runs { get; set; } = new();

    /// <summary>
    /// "numbered", "bullet", or null (plain text)
    /// </summary>
    [JsonPropertyName("bulletType")]
    public string? BulletType { get; set; }

    /// <summary>
    /// e.g. "arabicPeriod", "arabicParenR", "romanUcPeriod"
    /// </summary>
    [JsonPropertyName("numberingFormat")]
    public string? NumberingFormat { get; set; }

    [JsonPropertyName("indentLevel")]
    public int IndentLevel { get; set; }

    /// <summary>
    /// "left", "center", "right", "justify", or null
    /// </summary>
    [JsonPropertyName("alignment")]
    public string? Alignment { get; set; }
}
