using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using PptSemanticEditor.Core.Models;

using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace PptSemanticEditor.Renderer;

/// <summary>
/// Builds individual slides from semantic slide data.
/// </summary>
public class SlideBuilder
{
    private readonly ShapeBuilder _shapeBuilder = new();
    private readonly ConnectorBuilder _connectorBuilder = new();
    private readonly LayoutEngine _layoutEngine = new();

    public SlidePart CreateSlide(
        PresentationPart presentationPart,
        SemanticSlide semanticSlide,
        double slideWidth,
        double slideHeight,
        int slideIndex)
    {
        // Auto-layout elements without positions
        _layoutEngine.AutoLayout(semanticSlide, slideWidth, slideHeight);

        // Create slide part
        var slidePart = presentationPart.AddNewPart<SlidePart>($"rId{slideIndex + 2}");

        var slide = new P.Slide(
            new P.CommonSlideData(
                new P.ShapeTree(
                    new P.NonVisualGroupShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                        new P.NonVisualGroupShapeDrawingProperties(),
                        new P.ApplicationNonVisualDrawingProperties()
                    ),
                    new P.GroupShapeProperties(
                        new D.TransformGroup(
                            new D.Offset { X = 0, Y = 0 },
                            new D.Extents { Cx = 0, Cy = 0 },
                            new D.ChildOffset { X = 0, Y = 0 },
                            new D.ChildExtents { Cx = 0, Cy = 0 }
                        )
                    )
                )
            )
        );

        var shapeTree = slide.CommonSlideData!.ShapeTree!;
        uint nextId = 2;

        // Add elements as shapes
        foreach (var element in semanticSlide.Elements)
        {
            P.Shape? shape = element.Type.ToLowerInvariant() switch
            {
                "title" or "subtitle" => _shapeBuilder.CreateTitleShape(element, nextId),
                "text" or "body" => _shapeBuilder.CreateTextShape(element, nextId),
                "shape" => _shapeBuilder.CreateVisualShape(element, nextId),
                "image" => _shapeBuilder.CreateTextShape(element, nextId), // Fallback: render as text with label
                _ => _shapeBuilder.CreateTextShape(element, nextId)
            };

            if (shape != null)
            {
                shapeTree.Append(shape);
                nextId++;
            }
        }

        // Add connectors for relationships
        foreach (var relationship in semanticSlide.Relationships)
        {
            var fromElement = semanticSlide.Elements.FirstOrDefault(e => e.Id == relationship.From);
            var toElement = semanticSlide.Elements.FirstOrDefault(e => e.Id == relationship.To);

            var connector = _connectorBuilder.CreateConnector(relationship, fromElement, toElement, nextId);
            shapeTree.Append(connector);
            nextId++;
        }

        slidePart.Slide = slide;
        slidePart.Slide.Save();

        return slidePart;
    }
}
