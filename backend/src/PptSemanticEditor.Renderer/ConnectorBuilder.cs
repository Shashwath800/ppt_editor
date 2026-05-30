using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Presentation;
using PptSemanticEditor.Core.Models;

using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace PptSemanticEditor.Renderer;

/// <summary>
/// Builds connector shapes between elements.
/// </summary>
public class ConnectorBuilder
{
    private const long EmuPerInch = 914400;

    public P.ConnectionShape CreateConnector(
        SemanticRelationship relationship,
        SemanticElement? fromElement,
        SemanticElement? toElement,
        uint shapeId)
    {
        var cxnSp = new P.ConnectionShape();

        // Non-visual properties
        var nvCxnSpPr = new P.NonVisualConnectionShapeProperties(
            new P.NonVisualDrawingProperties { Id = shapeId, Name = relationship.Label ?? $"Connector {shapeId}" },
            new P.NonVisualConnectorShapeDrawingProperties(),
            new P.ApplicationNonVisualDrawingProperties()
        );

        // Calculate positions based on connected elements
        long startX = 0, startY = 0, endX = 0, endY = 0;

        if (fromElement != null)
        {
            startX = (long)((fromElement.X + fromElement.Width / 2) * EmuPerInch);
            startY = (long)((fromElement.Y + fromElement.Height) * EmuPerInch);
        }

        if (toElement != null)
        {
            endX = (long)((toElement.X + toElement.Width / 2) * EmuPerInch);
            endY = (long)(toElement.Y * EmuPerInch);
        }

        // Calculate offset and extent
        var offsetX = Math.Min(startX, endX);
        var offsetY = Math.Min(startY, endY);
        var extentCx = Math.Abs(endX - startX);
        var extentCy = Math.Abs(endY - startY);

        // Ensure minimum size
        if (extentCx < 1) extentCx = 1;
        if (extentCy < 1) extentCy = 1;

        var spPr = new P.ShapeProperties(
            new D.Transform2D(
                new D.Offset { X = offsetX, Y = offsetY },
                new D.Extents { Cx = extentCx, Cy = extentCy }
            ),
            new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.StraightConnector1 },
            new D.Outline(
                new D.SolidFill(new D.RgbColorModelHex { Val = "404040" }),
                new D.TailEnd { Type = D.LineEndValues.Triangle }
            )
            { Width = 12700 } // 1pt
        );

        cxnSp.Append(nvCxnSpPr);
        cxnSp.Append(spPr);

        return cxnSp;
    }
}
