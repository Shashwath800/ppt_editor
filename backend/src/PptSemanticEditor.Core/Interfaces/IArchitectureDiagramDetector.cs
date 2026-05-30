using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Core.Interfaces;

public class ArchitecturePattern
{
    public string PatternType { get; set; } = string.Empty;
    // "pipeline", "hierarchy", "star", "layered", "mesh"
    public double Confidence { get; set; }
    public string Description { get; set; } = string.Empty;
}

public interface IArchitectureDiagramDetector
{
    ArchitecturePattern? DetectPattern(SemanticGraph graph);
}
