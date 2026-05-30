using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Renderer;

/// <summary>
/// Provides auto-layout logic for slides that need positioning.
/// </summary>
public class LayoutEngine
{
    private const double DefaultSlideWidth = 10.0;  // inches
    private const double DefaultSlideHeight = 7.5;  // inches
    private const double Margin = 0.5;               // inches

    /// <summary>
    /// Auto-layout elements in a grid pattern if they have zero/default positions.
    /// </summary>
    public void AutoLayout(SemanticSlide slide, double slideWidth = DefaultSlideWidth, double slideHeight = DefaultSlideHeight)
    {
        var elementsNeedingLayout = slide.Elements
            .Where(e => e.X == 0 && e.Y == 0 && e.Width == 0 && e.Height == 0)
            .ToList();

        if (elementsNeedingLayout.Count == 0) return;

        switch (slide.Classification)
        {
            case "title_slide":
                LayoutTitleSlide(elementsNeedingLayout, slideWidth, slideHeight);
                break;
            case "flowchart":
            case "architecture_diagram":
                LayoutFlowchart(elementsNeedingLayout, slideWidth, slideHeight);
                break;
            default:
                LayoutGrid(elementsNeedingLayout, slideWidth, slideHeight);
                break;
        }
    }

    private void LayoutTitleSlide(List<SemanticElement> elements, double width, double height)
    {
        var centerX = width / 2;
        var centerY = height / 2;

        foreach (var element in elements)
        {
            var elementWidth = Math.Max(element.Width, 6.0);
            var elementHeight = Math.Max(element.Height, 1.5);

            element.Width = elementWidth;
            element.Height = elementHeight;
            element.X = centerX - elementWidth / 2;

            if (element.Type == "title")
            {
                element.Y = centerY - elementHeight - 0.5;
                element.FontSize = Math.Max(element.FontSize, 36);
            }
            else
            {
                element.Y = centerY + 0.5;
                element.FontSize = Math.Max(element.FontSize, 20);
            }
        }
    }

    private void LayoutFlowchart(List<SemanticElement> elements, double width, double height)
    {
        if (elements.Count == 0) return;

        var usableWidth = width - 2 * Margin;
        var usableHeight = height - 2 * Margin;

        // Arrange in a horizontal flow
        var cols = Math.Min(elements.Count, 4);
        var rows = (int)Math.Ceiling((double)elements.Count / cols);
        var cellWidth = usableWidth / cols;
        var cellHeight = usableHeight / rows;
        var shapeWidth = cellWidth * 0.7;
        var shapeHeight = cellHeight * 0.5;

        for (int i = 0; i < elements.Count; i++)
        {
            var row = i / cols;
            var col = i % cols;

            elements[i].X = Margin + col * cellWidth + (cellWidth - shapeWidth) / 2;
            elements[i].Y = Margin + row * cellHeight + (cellHeight - shapeHeight) / 2;
            elements[i].Width = shapeWidth;
            elements[i].Height = shapeHeight;
        }
    }

    private void LayoutGrid(List<SemanticElement> elements, double width, double height)
    {
        if (elements.Count == 0) return;

        var usableWidth = width - 2 * Margin;
        var usableHeight = height - 2 * Margin;

        // Find title element
        var title = elements.FirstOrDefault(e => e.Type == "title");
        var contentElements = elements.Where(e => e.Type != "title").ToList();

        if (title != null)
        {
            title.X = Margin;
            title.Y = Margin;
            title.Width = usableWidth;
            title.Height = 1.0;
            title.FontSize = Math.Max(title.FontSize, 28);
        }

        // Layout remaining content
        var startY = title != null ? Margin + 1.5 : Margin;
        var contentHeight = usableHeight - (title != null ? 1.5 : 0);

        if (contentElements.Count == 0) return;

        var cols = Math.Min(contentElements.Count, 2);
        var rows = (int)Math.Ceiling((double)contentElements.Count / cols);
        var cellWidth = usableWidth / cols;
        var cellHeight = contentHeight / rows;

        for (int i = 0; i < contentElements.Count; i++)
        {
            var row = i / cols;
            var col = i % cols;

            contentElements[i].X = Margin + col * cellWidth + 0.1;
            contentElements[i].Y = startY + row * cellHeight + 0.1;
            contentElements[i].Width = cellWidth - 0.2;
            contentElements[i].Height = cellHeight - 0.2;
        }
    }
}
