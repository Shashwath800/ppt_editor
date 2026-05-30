using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Semantic;

public class SlideClassifier : ISlideClassifier
{
    public SlideClassification Classify(OpenXmlSlideInfo slide)
    {
        var shapes = slide.Shapes;
        if (shapes.Count == 0)
            return SlideClassification.Unknown;

        var hasTitle = shapes.Any(s => s.IsTitle || s.Type == "title");
        var hasSubtitle = shapes.Any(s => s.Type == "subtitle");
        var hasBody = shapes.Any(s => s.Type == "body");
        var hasConnectors = shapes.Any(s => s.IsConnector);
        var hasTable = shapes.Any(s => s.Type == "table");
        var hasChart = shapes.Any(s => s.Type == "chart");
        var hasImages = shapes.Any(s => s.Type == "image");
        var textShapes = shapes.Where(s => !string.IsNullOrWhiteSpace(s.Text) && !s.IsConnector).ToList();
        var shapeCount = shapes.Count(s => s.Type == "shape" && !s.IsConnector);

        // Title slide: has title, possibly subtitle, minimal other content
        if (hasTitle && (hasSubtitle || textShapes.Count <= 2) && shapeCount <= 3 && !hasConnectors)
        {
            return SlideClassification.TitleSlide;
        }

        // Table slide
        if (hasTable)
        {
            return SlideClassification.TableSlide;
        }

        // Chart slide
        if (hasChart)
        {
            return SlideClassification.ChartSlide;
        }

        // Image slide: dominant image element
        if (hasImages && shapes.Count(s => s.Type == "image") >= shapes.Count / 2)
        {
            return SlideClassification.ImageSlide;
        }

        // Architecture diagram or flowchart: has connectors between shapes
        if (hasConnectors)
        {
            var connectorCount = shapes.Count(s => s.IsConnector);
            var nonConnectorShapes = shapes.Count(s => !s.IsConnector && s.Type == "shape");

            if (connectorCount >= 3 && nonConnectorShapes >= 4)
                return SlideClassification.ArchitectureDiagram;

            return SlideClassification.Flowchart;
        }

        // Bullet slide: has body text with multiple lines/paragraphs
        if (hasBody || textShapes.Any(s => s.Text.Contains('\n') && s.Text.Split('\n').Length > 2))
        {
            return SlideClassification.BulletSlide;
        }

        // Comparison slide: multiple side-by-side text blocks at similar Y positions
        if (textShapes.Count >= 4)
        {
            var yGroups = textShapes.GroupBy(s => Math.Round(s.OffsetY / 914400.0, 0));
            if (yGroups.Any(g => g.Count() >= 2))
                return SlideClassification.ComparisonSlide;
        }

        // Timeline: shapes arranged horizontally with similar Y
        if (shapeCount >= 3)
        {
            var yPositions = shapes.Where(s => s.Type == "shape" && !s.IsConnector)
                .Select(s => s.OffsetY)
                .Distinct()
                .ToList();

            if (yPositions.Count <= 2 && shapeCount >= 3)
                return SlideClassification.Timeline;
        }

        // Default: content slide
        return SlideClassification.ContentSlide;
    }
}
