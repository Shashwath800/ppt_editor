using System.Text;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using PptSemanticEditor.Core.Models;

using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;
using GroupShape = DocumentFormat.OpenXml.Presentation.GroupShape;
using ConnectionShape = DocumentFormat.OpenXml.Presentation.ConnectionShape;
using GraphicFrame = DocumentFormat.OpenXml.Presentation.GraphicFrame;
using NonVisualDrawingProperties = DocumentFormat.OpenXml.Presentation.NonVisualDrawingProperties;

namespace PptSemanticEditor.Parser;

public class ShapeExtractor
{
    private const double EmuPerInch = 914400.0;

    public List<OpenXmlShapeInfo> ExtractShapes(ShapeTree shapeTree, SlidePart slidePart)
    {
        var shapes = new List<OpenXmlShapeInfo>();

        foreach (var element in shapeTree.ChildElements)
        {
            switch (element)
            {
                case Shape shape:
                    shapes.Add(ExtractShape(shape));
                    break;
                case Picture picture:
                    shapes.Add(ExtractPicture(picture, slidePart));
                    break;
                case ConnectionShape connectionShape:
                    shapes.Add(ExtractConnectionShape(connectionShape));
                    break;
                case GraphicFrame graphicFrame:
                    shapes.Add(ExtractGraphicFrame(graphicFrame));
                    break;
                case GroupShape groupShape:
                    shapes.AddRange(ExtractGroupShape(groupShape, slidePart));
                    break;
            }
        }

        return shapes;
    }

    private OpenXmlShapeInfo ExtractShape(Shape shape)
    {
        var info = new OpenXmlShapeInfo
        {
            Type = "shape",
            RawXml = shape.OuterXml
        };

        // Get shape ID and name
        var nvSpPr = shape.NonVisualShapeProperties;
        if (nvSpPr?.NonVisualDrawingProperties != null)
        {
            var nvProps = nvSpPr.NonVisualDrawingProperties;
            info.ShapeId = nvProps.Id?.Value.ToString() ?? "";
            info.Name = nvProps.Name?.Value ?? "";
        }

        // Check if it's a title/subtitle placeholder
        var phType = nvSpPr?.ApplicationNonVisualDrawingProperties?
            .GetFirstChild<PlaceholderShape>()?.Type?.Value;
        if (phType != null)
        {
            info.IsTitle = phType == PlaceholderValues.Title ||
                          phType == PlaceholderValues.CenteredTitle;
            if (phType == PlaceholderValues.Title || phType == PlaceholderValues.CenteredTitle)
                info.Type = "title";
            else if (phType == PlaceholderValues.SubTitle)
                info.Type = "subtitle";
            else if (phType == PlaceholderValues.Body)
                info.Type = "body";
        }

        // Extract position and size
        ExtractTransform(shape.ShapeProperties?.Transform2D, info);

        // Extract text
        var textBody = shape.TextBody;
        if (textBody != null)
        {
            info.Text = ExtractTextContent(textBody);
            ExtractFontInfo(textBody, info);
        }

        // Extract fill color
        ExtractFillColor(shape.ShapeProperties, info);

        return info;
    }

    private OpenXmlShapeInfo ExtractPicture(Picture picture, SlidePart slidePart)
    {
        var info = new OpenXmlShapeInfo
        {
            Type = "image",
            RawXml = picture.OuterXml
        };

        var nvPicPr = picture.NonVisualPictureProperties;
        if (nvPicPr?.NonVisualDrawingProperties != null)
        {
            info.ShapeId = nvPicPr.NonVisualDrawingProperties.Id?.Value.ToString() ?? "";
            info.Name = nvPicPr.NonVisualDrawingProperties.Name?.Value ?? "";
        }

        ExtractTransform(picture.ShapeProperties?.Transform2D, info);

        return info;
    }

    private OpenXmlShapeInfo ExtractConnectionShape(ConnectionShape cxnSp)
    {
        var info = new OpenXmlShapeInfo
        {
            Type = "connector",
            IsConnector = true,
            RawXml = cxnSp.OuterXml
        };

        var nvCxnSpPr = cxnSp.NonVisualConnectionShapeProperties;
        if (nvCxnSpPr?.NonVisualDrawingProperties != null)
        {
            info.ShapeId = nvCxnSpPr.NonVisualDrawingProperties.Id?.Value.ToString() ?? "";
            info.Name = nvCxnSpPr.NonVisualDrawingProperties.Name?.Value ?? "";
        }

        // Extract connection endpoints
        var cxnPr = nvCxnSpPr?.NonVisualConnectorShapeDrawingProperties;
        var startCxn = cxnPr?.StartConnection;
        var endCxn = cxnPr?.EndConnection;

        if (startCxn?.Id?.Value != null)
            info.ConnectorStartId = startCxn.Id.Value.ToString();
        if (endCxn?.Id?.Value != null)
            info.ConnectorEndId = endCxn.Id.Value.ToString();

        ExtractTransform(cxnSp.ShapeProperties?.Transform2D, info);

        return info;
    }

    private OpenXmlShapeInfo ExtractGraphicFrame(GraphicFrame graphicFrame)
    {
        var info = new OpenXmlShapeInfo
        {
            RawXml = graphicFrame.OuterXml
        };

        var nvGfPr = graphicFrame.NonVisualGraphicFrameProperties;
        if (nvGfPr?.NonVisualDrawingProperties != null)
        {
            info.ShapeId = nvGfPr.NonVisualDrawingProperties.Id?.Value.ToString() ?? "";
            info.Name = nvGfPr.NonVisualDrawingProperties.Name?.Value ?? "";
        }

        // Determine the type based on content
        var outerXml = graphicFrame.OuterXml;
        if (outerXml.Contains("a:tbl") || outerXml.Contains("<a:tbl"))
            info.Type = "table";
        else if (outerXml.Contains("c:chart") || outerXml.Contains("chartSpace"))
            info.Type = "chart";
        else
            info.Type = "graphicFrame";

        // Extract transform from the graphic frame
        var xfrm = graphicFrame.Transform;
        if (xfrm != null)
        {
            if (xfrm.Offset != null)
            {
                info.OffsetX = xfrm.Offset.X?.Value ?? 0;
                info.OffsetY = xfrm.Offset.Y?.Value ?? 0;
            }
            if (xfrm.Extents != null)
            {
                info.ExtentCx = xfrm.Extents.Cx?.Value ?? 0;
                info.ExtentCy = xfrm.Extents.Cy?.Value ?? 0;
            }
        }

        return info;
    }

    private List<OpenXmlShapeInfo> ExtractGroupShape(GroupShape groupShape, SlidePart slidePart)
    {
        var shapes = new List<OpenXmlShapeInfo>();

        foreach (var child in groupShape.ChildElements)
        {
            switch (child)
            {
                case Shape shape:
                    shapes.Add(ExtractShape(shape));
                    break;
                case Picture picture:
                    shapes.Add(ExtractPicture(picture, slidePart));
                    break;
                case ConnectionShape cxnSp:
                    shapes.Add(ExtractConnectionShape(cxnSp));
                    break;
                case GraphicFrame gf:
                    shapes.Add(ExtractGraphicFrame(gf));
                    break;
            }
        }

        return shapes;
    }

    private void ExtractTransform(DocumentFormat.OpenXml.Drawing.Transform2D? transform, OpenXmlShapeInfo info)
    {
        if (transform == null) return;

        if (transform.Offset != null)
        {
            info.OffsetX = transform.Offset.X?.Value ?? 0;
            info.OffsetY = transform.Offset.Y?.Value ?? 0;
        }

        if (transform.Extents != null)
        {
            info.ExtentCx = transform.Extents.Cx?.Value ?? 0;
            info.ExtentCy = transform.Extents.Cy?.Value ?? 0;
        }
    }

    private string ExtractTextContent(OpenXmlCompositeElement textBody)
    {
        var sb = new StringBuilder();
        foreach (var paragraph in textBody.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>())
        {
            foreach (var run in paragraph.Elements<DocumentFormat.OpenXml.Drawing.Run>())
            {
                var text = run.GetFirstChild<DocumentFormat.OpenXml.Drawing.Text>();
                if (text != null)
                    sb.Append(text.Text);
            }
            // Also check for fields (like slide numbers, dates)
            foreach (var field in paragraph.Elements<DocumentFormat.OpenXml.Drawing.Field>())
            {
                var text = field.GetFirstChild<DocumentFormat.OpenXml.Drawing.Text>();
                if (text != null)
                    sb.Append(text.Text);
            }
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    private void ExtractFontInfo(OpenXmlCompositeElement textBody, OpenXmlShapeInfo info)
    {
        // Get font info from the first run
        var firstParagraph = textBody.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>().FirstOrDefault();
        var firstRun = firstParagraph?.Elements<DocumentFormat.OpenXml.Drawing.Run>().FirstOrDefault();
        var runProps = firstRun?.RunProperties;

        if (runProps != null)
        {
            // Font size in hundredths of a point → points
            if (runProps.FontSize?.Value != null)
                info.FontSize = runProps.FontSize.Value / 100.0;

            // Font color
            var solidFill = runProps.GetFirstChild<SolidFill>();
            if (solidFill != null)
            {
                var rgbColor = solidFill.GetFirstChild<RgbColorModelHex>();
                if (rgbColor?.Val?.Value != null)
                    info.FontColor = $"#{rgbColor.Val.Value}";
            }
        }

        // Also check default text properties
        if (info.FontSize == 0)
        {
            var defaultRunProps = firstParagraph?
                .GetFirstChild<DocumentFormat.OpenXml.Drawing.ParagraphProperties>()?
                .GetFirstChild<DefaultRunProperties>();
            if (defaultRunProps?.FontSize?.Value != null)
                info.FontSize = defaultRunProps.FontSize.Value / 100.0;
        }
    }

    private void ExtractFillColor(DocumentFormat.OpenXml.Presentation.ShapeProperties? shapeProps, OpenXmlShapeInfo info)
    {
        if (shapeProps == null) return;

        var solidFill = shapeProps.GetFirstChild<SolidFill>();
        if (solidFill != null)
        {
            var rgbColor = solidFill.GetFirstChild<RgbColorModelHex>();
            if (rgbColor?.Val?.Value != null)
            {
                info.FillColor = $"#{rgbColor.Val.Value}";
                return;
            }

            var schemeColor = solidFill.GetFirstChild<SchemeColor>();
            if (schemeColor?.Val?.Value != null)
            {
                info.FillColor = $"scheme:{schemeColor.Val.Value}";
            }
        }
    }
}
