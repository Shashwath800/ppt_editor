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
                    shapes.AddRange(ExtractGraphicFrame(graphicFrame));
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
            info.Paragraphs = ExtractParagraphs(textBody);
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

    private List<OpenXmlShapeInfo> ExtractGraphicFrame(GraphicFrame graphicFrame)
    {
        var results = new List<OpenXmlShapeInfo>();

        var parentInfo = new OpenXmlShapeInfo
        {
            RawXml = graphicFrame.OuterXml
        };

        var nvGfPr = graphicFrame.NonVisualGraphicFrameProperties;
        string tableShapeId = "";
        string tableName = "";
        if (nvGfPr?.NonVisualDrawingProperties != null)
        {
            tableShapeId = nvGfPr.NonVisualDrawingProperties.Id?.Value.ToString() ?? "";
            tableName = nvGfPr.NonVisualDrawingProperties.Name?.Value ?? "";
            parentInfo.ShapeId = tableShapeId;
            parentInfo.Name = tableName;
        }

        // Extract transform from the graphic frame (used by both table parent and cells)
        long offsetX = 0, offsetY = 0, extentCx = 0, extentCy = 0;
        var xfrm = graphicFrame.Transform;
        if (xfrm != null)
        {
            if (xfrm.Offset != null)
            {
                offsetX = xfrm.Offset.X?.Value ?? 0;
                offsetY = xfrm.Offset.Y?.Value ?? 0;
            }
            if (xfrm.Extents != null)
            {
                extentCx = xfrm.Extents.Cx?.Value ?? 0;
                extentCy = xfrm.Extents.Cy?.Value ?? 0;
            }
        }
        parentInfo.OffsetX = offsetX;
        parentInfo.OffsetY = offsetY;
        parentInfo.ExtentCx = extentCx;
        parentInfo.ExtentCy = extentCy;

        // Determine the type based on content
        var outerXml = graphicFrame.OuterXml;
        if (outerXml.Contains("a:tbl") || outerXml.Contains("<a:tbl"))
        {
            parentInfo.Type = "table";
            // Parent table entry has no text — cells are extracted individually below
            results.Add(parentInfo);

            // Walk the table structure: D.Table → D.TableRow → D.TableCell
            var table = graphicFrame.Descendants<DocumentFormat.OpenXml.Drawing.Table>().FirstOrDefault();
            if (table != null)
            {
                var rows = table.Elements<DocumentFormat.OpenXml.Drawing.TableRow>().ToList();
                for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
                {
                    var cells = rows[rowIdx].Elements<DocumentFormat.OpenXml.Drawing.TableCell>().ToList();
                    for (int colIdx = 0; colIdx < cells.Count; colIdx++)
                    {
                        var cell = cells[colIdx];

                        // Skip merge-continuation cells — they are empty placeholders
                        // whose content PowerPoint ignores. Only extract origin cells.
                        // Use typed properties instead of GetAttribute — the SDK throws
                        // KeyNotFoundException for attributes not in the element's schema.
                        if (cell.HorizontalMerge?.Value == true ||
                            cell.VerticalMerge?.Value == true)
                        {
                            continue;
                        }

                        var cellTextBody = cell.GetFirstChild<DocumentFormat.OpenXml.Drawing.TextBody>();
                        if (cellTextBody == null)
                            continue;

                        var cellText = ExtractTextContent(cellTextBody);
                        if (string.IsNullOrWhiteSpace(cellText))
                            continue;

                        var cellInfo = new OpenXmlShapeInfo
                        {
                            ShapeId = $"{tableShapeId}_r{rowIdx}_c{colIdx}",
                            Name = $"{tableName} R{rowIdx + 1}C{colIdx + 1}",
                            Type = "tableCell",
                            Text = cellText,
                            OffsetX = offsetX,
                            OffsetY = offsetY,
                            ExtentCx = extentCx,
                            ExtentCy = extentCy
                        };

                        ExtractFontInfo(cellTextBody, cellInfo);
                        cellInfo.Paragraphs = ExtractParagraphs(cellTextBody);

                        results.Add(cellInfo);
                    }
                }
            }
        }
        else if (outerXml.Contains("c:chart") || outerXml.Contains("chartSpace"))
        {
            parentInfo.Type = "chart";
            results.Add(parentInfo);
        }
        else
        {
            parentInfo.Type = "graphicFrame";
            results.Add(parentInfo);
        }

        return results;
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
                    shapes.AddRange(ExtractGraphicFrame(gf));
                    break;
                case GroupShape gs:
                    shapes.AddRange(ExtractGroupShape(gs, slidePart));
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
        var paragraphs = textBody.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>().ToList();
        
        for (int i = 0; i < paragraphs.Count; i++)
        {
            var paragraph = paragraphs[i];
            foreach (var child in paragraph.ChildElements)
            {
                if (child is DocumentFormat.OpenXml.Drawing.Run run)
                {
                    var text = run.GetFirstChild<DocumentFormat.OpenXml.Drawing.Text>();
                    if (text != null)
                        sb.Append(text.Text);
                }
                else if (child is DocumentFormat.OpenXml.Drawing.Field field)
                {
                    var text = field.GetFirstChild<DocumentFormat.OpenXml.Drawing.Text>();
                    if (text != null)
                        sb.Append(text.Text);
                }
                else if (child is DocumentFormat.OpenXml.Drawing.Break)
                {
                    sb.Append('\n');
                }
            }
            
            // Add newline between paragraphs (but not after the last one)
            if (i < paragraphs.Count - 1)
                sb.Append('\n');
        }
        return sb.ToString();
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

    /// <summary>
    /// Extracts per-paragraph, per-run formatting from a text body.
    /// Each paragraph captures bullet/numbering properties and alignment.
    /// Each run within a paragraph captures font color, size, bold, italic, and font family.
    /// </summary>
    private List<TextParagraph> ExtractParagraphs(OpenXmlCompositeElement textBody)
    {
        var result = new List<TextParagraph>();
        var paragraphs = textBody.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>().ToList();

        foreach (var paragraph in paragraphs)
        {
            var tp = new TextParagraph();

            // Extract paragraph properties (bullets, numbering, indent, alignment)
            var pPr = paragraph.GetFirstChild<DocumentFormat.OpenXml.Drawing.ParagraphProperties>();
            if (pPr != null)
            {
                // Alignment
                if (pPr.Alignment?.Value != null)
                {
                    var alignVal = pPr.Alignment.Value;
                    if (alignVal == DocumentFormat.OpenXml.Drawing.TextAlignmentTypeValues.Left)
                        tp.Alignment = "left";
                    else if (alignVal == DocumentFormat.OpenXml.Drawing.TextAlignmentTypeValues.Center)
                        tp.Alignment = "center";
                    else if (alignVal == DocumentFormat.OpenXml.Drawing.TextAlignmentTypeValues.Right)
                        tp.Alignment = "right";
                    else if (alignVal == DocumentFormat.OpenXml.Drawing.TextAlignmentTypeValues.Justified)
                        tp.Alignment = "justify";
                }

                // Indent level
                if (pPr.Level?.Value != null)
                    tp.IndentLevel = pPr.Level.Value;

                // Bullet/numbering detection
                var buAutoNum = pPr.GetFirstChild<DocumentFormat.OpenXml.Drawing.AutoNumberedBullet>();
                if (buAutoNum != null)
                {
                    tp.BulletType = "numbered";
                    tp.NumberingFormat = buAutoNum.Type?.Value.ToString();
                }
                else
                {
                    var buChar = pPr.GetFirstChild<DocumentFormat.OpenXml.Drawing.CharacterBullet>();
                    if (buChar != null)
                    {
                        tp.BulletType = "bullet";
                    }
                    else
                    {
                        // Check for picture bullets or other bullet types
                        var buNone = pPr.GetFirstChild<DocumentFormat.OpenXml.Drawing.NoBullet>();
                        if (buNone != null)
                            tp.BulletType = null; // explicitly no bullet
                    }
                }
            }

            // Extract runs with their formatting
            foreach (var run in paragraph.Elements<DocumentFormat.OpenXml.Drawing.Run>())
            {
                var tr = new TextRun();
                var text = run.GetFirstChild<DocumentFormat.OpenXml.Drawing.Text>();
                tr.Text = text?.Text ?? "";

                var rPr = run.RunProperties;
                if (rPr != null)
                {
                    // Font size (hundredths of a point → points)
                    if (rPr.FontSize?.Value != null)
                        tr.FontSize = rPr.FontSize.Value / 100.0;

                    // Bold
                    if (rPr.Bold?.Value != null)
                        tr.Bold = rPr.Bold.Value;

                    // Italic
                    if (rPr.Italic?.Value != null)
                        tr.Italic = rPr.Italic.Value;

                    // Font color
                    var solidFill = rPr.GetFirstChild<SolidFill>();
                    if (solidFill != null)
                    {
                        var rgbColor = solidFill.GetFirstChild<RgbColorModelHex>();
                        if (rgbColor?.Val?.Value != null)
                            tr.FontColor = $"#{rgbColor.Val.Value}";
                    }

                    // Font family
                    var latin = rPr.GetFirstChild<DocumentFormat.OpenXml.Drawing.LatinFont>();
                    if (latin?.Typeface != null)
                        tr.FontFamily = latin.Typeface;
                }

                tp.Runs.Add(tr);
            }

            result.Add(tp);
        }

        return result;
    }
}

