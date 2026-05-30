using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Semantic;

/// <summary>
/// Builds first-class SemanticGraph from slide shape data.
/// Combines explicit connector data with geometry-inferred edges.
/// </summary>
public class GraphBuilder : IGraphBuilder
{
    private const double EmuPerInch = 914400.0;
    private readonly IGeometryAnalyzer _geometryAnalyzer;
    private readonly IArchitectureDiagramDetector _archDetector;

    public GraphBuilder(IGeometryAnalyzer geometryAnalyzer, IArchitectureDiagramDetector archDetector)
    {
        _geometryAnalyzer = geometryAnalyzer;
        _archDetector = archDetector;
    }

    public SemanticGraph? BuildGraph(OpenXmlSlideInfo slide)
    {
        var shapes = slide.Shapes;
        var connectors = shapes.Where(s => s.IsConnector).ToList();
        var nodeShapes = shapes.Where(s => !s.IsConnector && s.Type == "shape" && s.ExtentCx > 0).ToList();

        if (nodeShapes.Count < 2) return null;

        var graph = new SemanticGraph();

        // Build nodes from non-connector shapes
        foreach (var shape in nodeShapes)
        {
            graph.Nodes.Add(new SemanticGraphNode
            {
                Id = shape.ShapeId,
                Label = !string.IsNullOrWhiteSpace(shape.Text)
                    ? shape.Text.Trim().Split('\n').First()
                    : shape.Name,
                X = Math.Round(shape.OffsetX / EmuPerInch, 3),
                Y = Math.Round(shape.OffsetY / EmuPerInch, 3),
                Width = Math.Round(shape.ExtentCx / EmuPerInch, 3),
                Height = Math.Round(shape.ExtentCy / EmuPerInch, 3),
                NodeType = InferNodeType(shape),
                Properties = new Dictionary<string, string>
                {
                    ["shapeId"] = shape.ShapeId,
                    ["fillColor"] = shape.FillColor ?? ""
                }
            });
        }

        // Pass 1: Explicit connector edges (confidence = 1.0)
        foreach (var connector in connectors)
        {
            if (!string.IsNullOrEmpty(connector.ConnectorStartId) &&
                !string.IsNullOrEmpty(connector.ConnectorEndId))
            {
                graph.Edges.Add(new SemanticGraphEdge
                {
                    From = connector.ConnectorStartId,
                    To = connector.ConnectorEndId,
                    Label = connector.Name ?? "",
                    EdgeType = "flow",
                    Confidence = 1.0
                });
            }
        }

        // Pass 2: Geometry-based inference if no explicit connectors
        if (!connectors.Any(c => !string.IsNullOrEmpty(c.ConnectorStartId)))
        {
            var inferredEdges = _geometryAnalyzer.InferEdges(shapes, 10.0, 7.5);
            foreach (var edge in inferredEdges)
            {
                // Only add if not already covered by explicit connector
                if (!graph.Edges.Any(e => e.From == edge.FromShapeId && e.To == edge.ToShapeId))
                {
                    graph.Edges.Add(new SemanticGraphEdge
                    {
                        From = edge.FromShapeId,
                        To = edge.ToShapeId,
                        EdgeType = "inferred",
                        Confidence = edge.Confidence
                    });
                }
            }
        }

        if (graph.Edges.Count == 0) return null;

        // Infer flow direction
        graph.FlowDirection = _geometryAnalyzer.InferFlowDirection(nodeShapes);

        // Detect architecture pattern
        var pattern = _archDetector.DetectPattern(graph);
        graph.GraphType = pattern?.PatternType ?? "flowchart";
        graph.Confidence = pattern?.Confidence ?? 0.7;

        return graph;
    }

    private static string InferNodeType(OpenXmlShapeInfo shape)
    {
        var name = shape.Name?.ToLowerInvariant() ?? "";
        var text = shape.Text?.ToLowerInvariant() ?? "";

        if (name.Contains("database") || text.Contains("db") || text.Contains("database")) return "database";
        if (name.Contains("cloud") || text.Contains("cloud") || text.Contains("aws") || text.Contains("azure")) return "cloud";
        if (name.Contains("service") || text.Contains("service") || text.Contains("api")) return "service";
        if (name.Contains("decision") || name.Contains("diamond")) return "decision";
        if (name.Contains("terminal") || name.Contains("start") || name.Contains("end")) return "terminal";

        return "process";
    }
}
