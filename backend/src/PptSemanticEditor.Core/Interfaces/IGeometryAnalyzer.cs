using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Core.Interfaces;

public class InferredEdge
{
    public string FromShapeId { get; set; } = string.Empty;
    public string ToShapeId { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string InferenceMethod { get; set; } = string.Empty;
    // "proximity", "arrow_direction", "overlap", "nearest_neighbor"
}

public interface IGeometryAnalyzer
{
    List<InferredEdge> InferEdges(List<OpenXmlShapeInfo> shapes, double slideWidth, double slideHeight);
    string InferFlowDirection(List<OpenXmlShapeInfo> shapes);
}
