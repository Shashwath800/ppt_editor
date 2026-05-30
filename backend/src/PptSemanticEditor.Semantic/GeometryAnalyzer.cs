using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Semantic;

/// <summary>
/// Geometry-based edge inference for slides where shapes are connected
/// by visual arrow shapes (no formal connector metadata).
/// </summary>
public class GeometryAnalyzer : IGeometryAnalyzer
{
    private const double EmuPerInch = 914400.0;

    // Proximity threshold in inches — shapes within this distance get an inferred edge
    private const double ProximityThreshold = 0.6;

    // Minimum confidence for an inferred edge to be included
    private const double MinConfidence = 0.5;

    public List<InferredEdge> InferEdges(List<OpenXmlShapeInfo> shapes, double slideWidth, double slideHeight)
    {
        var inferred = new List<InferredEdge>();
        var candidates = shapes.Where(s => !s.IsConnector && s.Type == "shape" && s.ExtentCx > 0).ToList();

        // Pass 1: Arrow-shape detection
        // Arrow shapes that straddle between two shapes
        var arrowShapes = shapes.Where(s => s.IsConnector || IsArrowPreset(s)).ToList();
        foreach (var arrow in arrowShapes)
        {
            var edges = InferFromArrow(arrow, candidates);
            inferred.AddRange(edges);
        }

        // Pass 2: Proximity-based inference (for shapes with NO connectors at all)
        if (!arrowShapes.Any())
        {
            var proxEdges = InferFromProximity(candidates);
            inferred.AddRange(proxEdges);
        }

        // Deduplicate
        return inferred
            .GroupBy(e => $"{e.FromShapeId}->{e.ToShapeId}")
            .Select(g => g.OrderByDescending(e => e.Confidence).First())
            .Where(e => e.Confidence >= MinConfidence)
            .ToList();
    }

    public string InferFlowDirection(List<OpenXmlShapeInfo> shapes)
    {
        var candidates = shapes
            .Where(s => !s.IsConnector && s.Type == "shape" && s.ExtentCx > 0)
            .OrderBy(s => s.OffsetX)
            .ToList();

        if (candidates.Count < 2) return "unknown";

        // Calculate spread in X vs Y
        var xSpread = (double)(candidates.Max(s => s.OffsetX) - candidates.Min(s => s.OffsetX));
        var ySpread = (double)(candidates.Max(s => s.OffsetY) - candidates.Min(s => s.OffsetY));

        if (xSpread == 0 && ySpread == 0) return "unknown";

        var ratio = xSpread / Math.Max(ySpread, 1);

        if (ratio > 1.5) return "left_to_right";
        if (ratio < 0.67) return "top_to_bottom";

        // Check if arrows point right or down
        return xSpread >= ySpread ? "left_to_right" : "top_to_bottom";
    }

    private List<InferredEdge> InferFromArrow(OpenXmlShapeInfo arrow, List<OpenXmlShapeInfo> candidates)
    {
        var result = new List<InferredEdge>();

        // Arrow midpoint
        var arrowMidX = arrow.OffsetX + arrow.ExtentCx / 2.0;
        var arrowMidY = arrow.OffsetY + arrow.ExtentCy / 2.0;

        // Arrow start (left/top end) and end (right/bottom end)
        var arrowStartX = arrow.OffsetX;
        var arrowStartY = arrow.OffsetY;
        var arrowEndX = arrow.OffsetX + arrow.ExtentCx;
        var arrowEndY = arrow.OffsetY + arrow.ExtentCy;

        OpenXmlShapeInfo? fromShape = null;
        OpenXmlShapeInfo? toShape = null;
        double bestFromDist = double.MaxValue;
        double bestToDist = double.MaxValue;

        foreach (var shape in candidates)
        {
            // Shape center
            var shapeCenterX = shape.OffsetX + shape.ExtentCx / 2.0;
            var shapeCenterY = shape.OffsetY + shape.ExtentCy / 2.0;

            // Distance from arrow start to shape center
            var startDist = Distance(arrowStartX, arrowStartY, shapeCenterX, shapeCenterY);
            // Distance from arrow end to shape center
            var endDist = Distance(arrowEndX, arrowEndY, shapeCenterX, shapeCenterY);

            if (startDist < bestFromDist)
            {
                bestFromDist = startDist;
                fromShape = shape;
            }
            if (endDist < bestToDist && shape.ShapeId != fromShape?.ShapeId)
            {
                bestToDist = endDist;
                toShape = shape;
            }
        }

        if (fromShape != null && toShape != null && fromShape.ShapeId != toShape.ShapeId)
        {
            var thresholdEmu = ProximityThreshold * EmuPerInch;
            var confidence = Math.Max(0,
                1.0 - (bestFromDist + bestToDist) / (2.0 * thresholdEmu * 3));

            if (confidence > MinConfidence)
            {
                result.Add(new InferredEdge
                {
                    FromShapeId = fromShape.ShapeId,
                    ToShapeId = toShape.ShapeId,
                    Confidence = Math.Min(confidence, 0.95),
                    InferenceMethod = "arrow_direction"
                });
            }
        }

        return result;
    }

    private List<InferredEdge> InferFromProximity(List<OpenXmlShapeInfo> shapes)
    {
        var result = new List<InferredEdge>();
        var thresholdEmu = ProximityThreshold * EmuPerInch;

        // Sort by X then Y (left-to-right reading order)
        var sorted = shapes.OrderBy(s => s.OffsetX).ThenBy(s => s.OffsetY).ToList();

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            var current = sorted[i];
            var next = sorted[i + 1];

            var currentRight = current.OffsetX + current.ExtentCx;
            var currentBottom = current.OffsetY + current.ExtentCy;
            var gapX = next.OffsetX - currentRight;
            var gapY = next.OffsetY - currentBottom;

            // Horizontal adjacency
            if (gapX >= 0 && gapX <= thresholdEmu)
            {
                var confidence = 1.0 - (gapX / thresholdEmu) * 0.5;
                result.Add(new InferredEdge
                {
                    FromShapeId = current.ShapeId,
                    ToShapeId = next.ShapeId,
                    Confidence = confidence * 0.75, // proximity is less confident
                    InferenceMethod = "proximity"
                });
            }
            // Vertical adjacency
            else if (gapY >= 0 && gapY <= thresholdEmu)
            {
                var confidence = 1.0 - (gapY / thresholdEmu) * 0.5;
                result.Add(new InferredEdge
                {
                    FromShapeId = current.ShapeId,
                    ToShapeId = next.ShapeId,
                    Confidence = confidence * 0.7,
                    InferenceMethod = "proximity"
                });
            }
        }

        return result;
    }

    private static bool IsArrowPreset(OpenXmlShapeInfo shape)
    {
        // Arrow shapes typically have "Arrow" or "Connector" in their name
        // or are very narrow (width >> height or height >> width)
        var name = shape.Name?.ToLowerInvariant() ?? "";
        if (name.Contains("arrow") || name.Contains("connector")) return true;
        if (shape.ExtentCx > 0 && shape.ExtentCy > 0)
        {
            var ratio = (double)shape.ExtentCx / shape.ExtentCy;
            // Very elongated shapes are likely arrows
            return ratio > 5 || ratio < 0.2;
        }
        return false;
    }

    private static double Distance(double x1, double y1, double x2, double y2)
        => Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
}
