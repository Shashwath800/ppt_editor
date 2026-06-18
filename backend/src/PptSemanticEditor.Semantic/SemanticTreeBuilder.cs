using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Semantic;

public class SemanticTreeBuilder : ISemanticTreeBuilder
{
    private const double EmuPerInch = 914400.0;
    private readonly ISlideClassifier _classifier;
    private readonly IGraphDetector _graphDetector;
    private readonly IGraphBuilder _graphBuilder;

    public SemanticTreeBuilder(ISlideClassifier classifier, IGraphDetector graphDetector, IGraphBuilder graphBuilder)
    {
        _classifier = classifier;
        _graphDetector = graphDetector;
        _graphBuilder = graphBuilder;
    }

    public SemanticPresentation BuildTree(OpenXmlInfo openXmlInfo)
    {
        var presentation = new SemanticPresentation
        {
            FileName = openXmlInfo.FileName,
            SlideWidth = openXmlInfo.SlideWidth,
            SlideHeight = openXmlInfo.SlideHeight
        };

        foreach (var slideInfo in openXmlInfo.Slides)
        {
            var semanticSlide = BuildSlide(slideInfo, openXmlInfo.SlideWidth, openXmlInfo.SlideHeight);
            presentation.Slides.Add(semanticSlide);
        }

        presentation.SlideCount = presentation.Slides.Count;
        return presentation;
    }

    private SemanticSlide BuildSlide(OpenXmlSlideInfo slideInfo, double slideWidth, double slideHeight)
    {
        var classification = _classifier.Classify(slideInfo);

        var slide = new SemanticSlide
        {
            Id = slideInfo.SlideIndex + 1, // 1-based for display
            Classification = classification.ToFriendlyString(),
            ClassificationType = classification.ToDisplayName()
        };

        // Convert shapes to semantic elements
        foreach (var shape in slideInfo.Shapes)
        {
            if (shape.IsConnector) continue; // Handle connectors as relationships

            var element = ConvertToElement(shape, slideWidth, slideHeight);
            slide.Elements.Add(element);
        }

        // Find title
        var titleElement = slide.Elements.FirstOrDefault(e =>
            e.Type == "title" || e.Properties.ContainsKey("isTitle"));
        slide.Title = titleElement?.Text ?? GetInferredTitle(slide.Elements);

        // Build relationships from connectors
        foreach (var shape in slideInfo.Shapes.Where(s => s.IsConnector))
        {
            var rel = BuildRelationship(shape, slideInfo.Shapes);
            if (rel != null)
                slide.Relationships.Add(rel);
        }

        // Also check graph detection for architecture diagrams
        var graphData = _graphDetector.DetectGraph(slideInfo);
        if (graphData != null && graphData.Edges.Count > 0)
        {
            foreach (var edge in graphData.Edges)
            {
                // Avoid duplicates
                if (!slide.Relationships.Any(r => r.From == edge.From && r.To == edge.To))
                {
                    slide.Relationships.Add(new SemanticRelationship
                    {
                        From = edge.From,
                        To = edge.To,
                        Label = edge.Label,
                        Type = "graph_edge"
                    });
                }
            }
        }

        // Build first-class SemanticGraph for graph-type slides
        var semanticGraph = _graphBuilder.BuildGraph(slideInfo);
        if (semanticGraph != null)
        {
            slide.Graph = semanticGraph;
            // Override classification for identified graph types
            if (semanticGraph.GraphType == "pipeline")
                slide.Classification = "flowchart";
            else if (semanticGraph.GraphType == "hierarchy")
                slide.Classification = "architecture_diagram";
        }

        return slide;
    }

    private SemanticElement ConvertToElement(OpenXmlShapeInfo shape, double slideWidth, double slideHeight)
    {
        return new SemanticElement
        {
            Id = $"element_{shape.ShapeId}",
            Type = MapShapeType(shape.Type),
            Label = shape.Name,
            Text = shape.Text,
            X = Math.Round(shape.OffsetX / EmuPerInch, 3),
            Y = Math.Round(shape.OffsetY / EmuPerInch, 3),
            Width = Math.Round(shape.ExtentCx / EmuPerInch, 3),
            Height = Math.Round(shape.ExtentCy / EmuPerInch, 3),
            FontSize = shape.FontSize > 0 ? shape.FontSize : 18,
            FontColor = !string.IsNullOrEmpty(shape.FontColor) ? shape.FontColor : "#000000",
            FillColor = !string.IsNullOrEmpty(shape.FillColor) ? shape.FillColor : "transparent",
            ImageBase64 = shape.ImageBase64,
            Properties = BuildProperties(shape),
            Paragraphs = shape.Paragraphs
        };
    }

    private string MapShapeType(string rawType)
    {
        return rawType.ToLowerInvariant() switch
        {
            "title" => "title",
            "subtitle" => "subtitle",
            "body" => "text",
            "shape" => "shape",
            "image" => "image",
            "table" => "table",
            "tablecell" => "tableCell",
            "chart" => "chart",
            "graphicframe" => "shape",
            _ => "shape"
        };
    }

    private Dictionary<string, string> BuildProperties(OpenXmlShapeInfo shape)
    {
        var props = new Dictionary<string, string>();

        if (shape.IsTitle)
            props["isTitle"] = "true";
        if (!string.IsNullOrEmpty(shape.ConnectorStartId))
            props["connectorStart"] = shape.ConnectorStartId;
        if (!string.IsNullOrEmpty(shape.ConnectorEndId))
            props["connectorEnd"] = shape.ConnectorEndId;
        if (shape.Type == "table")
            props["elementType"] = "table";
        if (shape.Type == "chart")
            props["elementType"] = "chart";

        return props;
    }

    private SemanticRelationship? BuildRelationship(OpenXmlShapeInfo connector, List<OpenXmlShapeInfo> allShapes)
    {
        if (string.IsNullOrEmpty(connector.ConnectorStartId) ||
            string.IsNullOrEmpty(connector.ConnectorEndId))
            return null;

        var fromShape = allShapes.FirstOrDefault(s => s.ShapeId == connector.ConnectorStartId);
        var toShape = allShapes.FirstOrDefault(s => s.ShapeId == connector.ConnectorEndId);

        return new SemanticRelationship
        {
            From = $"element_{connector.ConnectorStartId}",
            To = $"element_{connector.ConnectorEndId}",
            Label = connector.Name ?? "connects to",
            Type = "connector"
        };
    }

    private string GetInferredTitle(List<SemanticElement> elements)
    {
        // Try to infer title from the first large text element
        var candidate = elements
            .Where(e => !string.IsNullOrWhiteSpace(e.Text) && e.Text.Length < 200)
            .OrderByDescending(e => e.FontSize)
            .ThenBy(e => e.Y)
            .FirstOrDefault();

        return candidate?.Text?.Split('\n').FirstOrDefault()?.Trim() ?? "Untitled Slide";
    }
}
