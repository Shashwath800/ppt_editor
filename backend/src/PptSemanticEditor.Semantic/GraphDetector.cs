using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Semantic;

public class GraphDetector : IGraphDetector
{
    private const double EmuPerInch = 914400.0;

    public GraphData? DetectGraph(OpenXmlSlideInfo slide)
    {
        var connectors = slide.Shapes.Where(s => s.IsConnector).ToList();
        var nonConnectorShapes = slide.Shapes.Where(s => !s.IsConnector && s.Type == "shape").ToList();

        if (connectors.Count == 0)
            return null;

        var graphData = new GraphData();

        // Build nodes from non-connector shapes
        foreach (var shape in nonConnectorShapes)
        {
            graphData.Nodes.Add(new GraphNode
            {
                Id = shape.ShapeId,
                Label = !string.IsNullOrWhiteSpace(shape.Text) ? shape.Text.Trim() : shape.Name,
                X = shape.OffsetX / EmuPerInch,
                Y = shape.OffsetY / EmuPerInch
            });
        }

        // Build edges from connectors
        foreach (var connector in connectors)
        {
            if (!string.IsNullOrEmpty(connector.ConnectorStartId) &&
                !string.IsNullOrEmpty(connector.ConnectorEndId))
            {
                graphData.Edges.Add(new GraphEdge
                {
                    From = connector.ConnectorStartId,
                    To = connector.ConnectorEndId,
                    Label = connector.Name ?? ""
                });
            }
        }

        // Only return if we found a meaningful graph (at least 2 nodes with edges)
        if (graphData.Nodes.Count >= 2 && graphData.Edges.Count >= 1)
            return graphData;

        return null;
    }
}
