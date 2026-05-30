using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using PptSemanticEditor.Core.Models;

using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace PptSemanticEditor.Renderer;

/// <summary>
/// Builds individual shape elements for slides.
/// </summary>
public class ShapeBuilder
{
    private const long EmuPerInch = 914400;

    /// <summary>
    /// Creates a text shape from a semantic element.
    /// </summary>
    public P.Shape CreateTextShape(SemanticElement element, uint shapeId)
    {
        var shape = new P.Shape();

        // Non-visual shape properties
        var nvSpPr = new P.NonVisualShapeProperties(
            new P.NonVisualDrawingProperties { Id = shapeId, Name = element.Label ?? $"Shape {shapeId}" },
            new P.NonVisualShapeDrawingProperties(),
            new P.ApplicationNonVisualDrawingProperties()
        );

        // Shape properties with position and size
        var spPr = new P.ShapeProperties(
            new D.Transform2D(
                new D.Offset
                {
                    X = (long)(element.X * EmuPerInch),
                    Y = (long)(element.Y * EmuPerInch)
                },
                new D.Extents
                {
                    Cx = (long)(element.Width * EmuPerInch),
                    Cy = (long)(element.Height * EmuPerInch)
                }
            ),
            new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle }
        );

        // Add fill color if not transparent
        if (!string.IsNullOrEmpty(element.FillColor) && element.FillColor != "transparent")
        {
            var fillColor = element.FillColor.TrimStart('#');
            if (fillColor.Length == 6)
            {
                spPr.AppendChild(new D.SolidFill(
                    new D.RgbColorModelHex { Val = fillColor }
                ));
            }
        }
        else
        {
            spPr.AppendChild(new D.NoFill());
        }

        // Text body
        var textBody = CreateTextBody(element);

        shape.Append(nvSpPr);
        shape.Append(spPr);
        shape.Append(textBody);

        return shape;
    }

    /// <summary>
    /// Creates a title shape with larger default font.
    /// </summary>
    public P.Shape CreateTitleShape(SemanticElement element, uint shapeId)
    {
        // Override font size for titles if not explicitly set
        if (element.FontSize < 24)
            element.FontSize = 36;

        return CreateTextShape(element, shapeId);
    }

    /// <summary>
    /// Creates a visual shape (rectangle, etc.) from a semantic element.
    /// </summary>
    public P.Shape CreateVisualShape(SemanticElement element, uint shapeId)
    {
        var shape = new P.Shape();

        var nvSpPr = new P.NonVisualShapeProperties(
            new P.NonVisualDrawingProperties { Id = shapeId, Name = element.Label ?? $"Shape {shapeId}" },
            new P.NonVisualShapeDrawingProperties(),
            new P.ApplicationNonVisualDrawingProperties()
        );

        var spPr = new P.ShapeProperties(
            new D.Transform2D(
                new D.Offset
                {
                    X = (long)(element.X * EmuPerInch),
                    Y = (long)(element.Y * EmuPerInch)
                },
                new D.Extents
                {
                    Cx = (long)(element.Width * EmuPerInch),
                    Cy = (long)(element.Height * EmuPerInch)
                }
            ),
            new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.RoundRectangle }
        );

        // Fill color
        var fillColor = element.FillColor?.TrimStart('#') ?? "4472C4";
        if (fillColor.Length == 6)
        {
            spPr.AppendChild(new D.SolidFill(
                new D.RgbColorModelHex { Val = fillColor }
            ));
        }

        // Outline
        spPr.AppendChild(new D.Outline(
            new D.SolidFill(new D.RgbColorModelHex { Val = "2F5597" })
        )
        { Width = 12700 }); // 1pt

        // Add text if present
        P.TextBody? textBody = null;
        if (!string.IsNullOrWhiteSpace(element.Text))
        {
            textBody = CreateTextBody(element);
        }

        shape.Append(nvSpPr);
        shape.Append(spPr);
        if (textBody != null) shape.Append(textBody);

        return shape;
    }

    private P.TextBody CreateTextBody(SemanticElement element)
    {
        var fontSize = (int)(element.FontSize > 0 ? element.FontSize * 100 : 1800); // hundredths of a point
        var fontColor = element.FontColor?.TrimStart('#') ?? "000000";
        if (fontColor.Length != 6) fontColor = "000000";

        var textBody = new P.TextBody(
            new D.BodyProperties
            {
                Wrap = D.TextWrappingValues.Square,
                Anchor = D.TextAnchoringTypeValues.Center
            },
            new D.ListStyle()
        );

        // Split text into paragraphs by newline
        var lines = (element.Text ?? string.Empty).Split('\n');
        foreach (var line in lines)
        {
            var paragraph = new D.Paragraph(
                new D.ParagraphProperties { Alignment = D.TextAlignmentTypeValues.Center },
                new D.Run(
                    new D.RunProperties(
                        new D.SolidFill(new D.RgbColorModelHex { Val = fontColor }),
                        new D.LatinFont { Typeface = "Calibri" }
                    )
                    {
                        FontSize = fontSize,
                        Language = "en-US",
                        Dirty = false
                    },
                    new D.Text(line)
                )
            );
            textBody.Append(paragraph);
        }

        return textBody;
    }
}
