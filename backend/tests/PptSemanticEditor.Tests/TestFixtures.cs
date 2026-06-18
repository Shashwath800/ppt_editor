using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace PptSemanticEditor.Tests;

/// <summary>
/// Builds minimal, valid .pptx files in memory for test fixtures.
/// Each method returns a MemoryStream containing a valid PPTX.
/// </summary>
public static class TestFixtures
{
    /// <summary>
    /// Creates a PPTX with one slide containing a 2x2 table.
    /// Cells contain: "Cell A1", "Cell B1", "Cell A2", "Cell B2".
    /// Table shape ID = 100.
    /// </summary>
    public static MemoryStream CreateTablePresentation()
    {
        var stream = new MemoryStream();
        using (var doc = PresentationDocument.Create(stream, PresentationDocumentType.Presentation, true))
        {
            var presentationPart = doc.AddPresentationPart();
            presentationPart.Presentation = new Presentation(
                new SlideIdList(),
                new SlideSize { Cx = 9144000, Cy = 6858000 },
                new NotesSize { Cx = 6858000, Cy = 9144000 }
            );

            var slidePart = presentationPart.AddNewPart<SlidePart>();
            var slideId = new SlideId { Id = 256, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
            presentationPart.Presentation.SlideIdList!.Append(slideId);

            // Build table
            var table = new D.Table();
            table.AppendChild(new D.TableProperties { FirstRow = true, BandRow = true });
            table.AppendChild(new D.TableGrid(
                new D.GridColumn { Width = 3000000 },
                new D.GridColumn { Width = 3000000 }
            ));

            // Row 0: "Cell A1", "Cell B1"
            table.AppendChild(CreateTableRow("Cell A1", "Cell B1", 370840));
            // Row 1: "Cell A2", "Cell B2"
            table.AppendChild(CreateTableRow("Cell A2", "Cell B2", 370840));

            var graphicData = new D.GraphicData(
                new D.Table[] { table }.First()
            ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/table" };

            var graphic = new D.Graphic(graphicData);

            var graphicFrame = new GraphicFrame(
                new NonVisualGraphicFrameProperties(
                    new NonVisualDrawingProperties { Id = 100, Name = "Table 1" },
                    new NonVisualGraphicFrameDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()
                ),
                new Transform(
                    new D.Offset { X = 500000, Y = 500000 },
                    new D.Extents { Cx = 6000000, Cy = 741680 }
                ),
                graphic
            );

            slidePart.Slide = new Slide(
                new CommonSlideData(new ShapeTree(
                    new NonVisualGroupShapeProperties(
                        new NonVisualDrawingProperties { Id = 1, Name = "" },
                        new NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()
                    ),
                    new GroupShapeProperties(new D.TransformGroup()),
                    graphicFrame
                ))
            );

            presentationPart.Presentation.Save();
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Creates a PPTX with one slide containing two shape boxes ("Start", "Process")
    /// connected by a straight connector. Shape IDs = 10, 11. Connector ID = 12.
    /// </summary>
    public static MemoryStream CreateFlowchartPresentation()
    {
        var stream = new MemoryStream();
        using (var doc = PresentationDocument.Create(stream, PresentationDocumentType.Presentation, true))
        {
            var presentationPart = doc.AddPresentationPart();
            presentationPart.Presentation = new Presentation(
                new SlideIdList(),
                new SlideSize { Cx = 9144000, Cy = 6858000 },
                new NotesSize { Cx = 6858000, Cy = 9144000 }
            );

            var slidePart = presentationPart.AddNewPart<SlidePart>();
            var slideId = new SlideId { Id = 256, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
            presentationPart.Presentation.SlideIdList!.Append(slideId);

            // Shape 1: "Start"
            var shape1 = CreateTextShape(10, "Start Box", "Start", 500000, 500000, 2000000, 500000);
            // Shape 2: "Process"
            var shape2 = CreateTextShape(11, "Process Box", "Process", 4000000, 500000, 2000000, 500000);
            // Connector between them
            var connector = CreateConnector(12, "Connector 1", 10, 11);

            slidePart.Slide = new Slide(
                new CommonSlideData(new ShapeTree(
                    new NonVisualGroupShapeProperties(
                        new NonVisualDrawingProperties { Id = 1, Name = "" },
                        new NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()
                    ),
                    new GroupShapeProperties(new D.TransformGroup()),
                    shape1, shape2, connector
                ))
            );

            presentationPart.Presentation.Save();
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Creates a PPTX with one slide containing a shape with two differently-formatted runs:
    /// Run 1: "Bold Red" (bold, red #FF0000)
    /// Run 2: " Plain Black" (normal, black #000000)
    /// Shape ID = 20.
    /// </summary>
    public static MemoryStream CreateMultiRunPresentation()
    {
        var stream = new MemoryStream();
        using (var doc = PresentationDocument.Create(stream, PresentationDocumentType.Presentation, true))
        {
            var presentationPart = doc.AddPresentationPart();
            presentationPart.Presentation = new Presentation(
                new SlideIdList(),
                new SlideSize { Cx = 9144000, Cy = 6858000 },
                new NotesSize { Cx = 6858000, Cy = 9144000 }
            );

            var slidePart = presentationPart.AddNewPart<SlidePart>();
            var slideId = new SlideId { Id = 256, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
            presentationPart.Presentation.SlideIdList!.Append(slideId);

            // Two-run paragraph
            var run1Props = new D.RunProperties(
                new D.SolidFill(new D.RgbColorModelHex { Val = "FF0000" })
            ) { Language = "en-US", Bold = true, FontSize = 1800, Dirty = false };

            var run2Props = new D.RunProperties(
                new D.SolidFill(new D.RgbColorModelHex { Val = "000000" })
            ) { Language = "en-US", Bold = false, FontSize = 1800, Dirty = false };

            var paragraph = new D.Paragraph(
                new D.Run(run1Props, new D.Text("Bold Red")),
                new D.Run(run2Props, new D.Text(" Plain Black")),
                new D.EndParagraphRunProperties { Language = "en-US" }
            );

            var textBody = new P.TextBody(
                new D.BodyProperties(),
                new D.ListStyle(),
                paragraph
            );

            var shape = new P.Shape(
                new NonVisualShapeProperties(
                    new NonVisualDrawingProperties { Id = 20, Name = "MultiRun Shape" },
                    new NonVisualShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()
                ),
                new P.ShapeProperties(
                    new D.Transform2D(
                        new D.Offset { X = 500000, Y = 500000 },
                        new D.Extents { Cx = 4000000, Cy = 500000 }
                    )
                ),
                textBody
            );

            slidePart.Slide = new Slide(
                new CommonSlideData(new ShapeTree(
                    new NonVisualGroupShapeProperties(
                        new NonVisualDrawingProperties { Id = 1, Name = "" },
                        new NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()
                    ),
                    new GroupShapeProperties(new D.TransformGroup()),
                    shape
                ))
            );

            presentationPart.Presentation.Save();
        }

        stream.Position = 0;
        return stream;
    }

    // --- Helper methods ---

    private static D.TableRow CreateTableRow(string cell1Text, string cell2Text, long height)
    {
        var row = new D.TableRow { Height = height };
        row.AppendChild(CreateTableCell(cell1Text));
        row.AppendChild(CreateTableCell(cell2Text));
        return row;
    }

    private static D.TableCell CreateTableCell(string text)
    {
        var cell = new D.TableCell(
            new D.TextBody(
                new D.BodyProperties(),
                new D.ListStyle(),
                new D.Paragraph(
                    new D.Run(
                        new D.RunProperties { Language = "en-US", FontSize = 1400, Dirty = false },
                        new D.Text(text)
                    ),
                    new D.EndParagraphRunProperties { Language = "en-US" }
                )
            ),
            new D.TableCellProperties()
        );
        return cell;
    }

    private static P.Shape CreateTextShape(uint id, string name, string text,
        long x, long y, long cx, long cy)
    {
        return new P.Shape(
            new NonVisualShapeProperties(
                new NonVisualDrawingProperties { Id = id, Name = name },
                new NonVisualShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()
            ),
            new P.ShapeProperties(
                new D.Transform2D(
                    new D.Offset { X = x, Y = y },
                    new D.Extents { Cx = cx, Cy = cy }
                )
            ),
            new P.TextBody(
                new D.BodyProperties(),
                new D.ListStyle(),
                new D.Paragraph(
                    new D.Run(
                        new D.RunProperties { Language = "en-US", FontSize = 1800, Dirty = false },
                        new D.Text(text)
                    ),
                    new D.EndParagraphRunProperties { Language = "en-US" }
                )
            )
        );
    }

    private static ConnectionShape CreateConnector(uint id, string name, uint startId, uint endId)
    {
        return new ConnectionShape(
            new NonVisualConnectionShapeProperties(
                new NonVisualDrawingProperties { Id = id, Name = name },
                new NonVisualConnectorShapeDrawingProperties(
                    new D.ConnectionShapeLocks(),
                    new D.StartConnection { Id = startId, Index = 2 },
                    new D.EndConnection { Id = endId, Index = 0 }
                ),
                new ApplicationNonVisualDrawingProperties()
            ),
            new P.ShapeProperties(
                new D.Transform2D(
                    new D.Offset { X = 2500000, Y = 750000 },
                    new D.Extents { Cx = 1500000, Cy = 0 }
                ),
                new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Line }
            )
        );
    }

    /// <summary>
    /// Creates a PPTX with one slide containing a 2x2 table with a horizontal merge on Row 0 (Col 0 and Col 1).
    /// </summary>
    public static MemoryStream CreateMergedTablePresentation()
    {
        var stream = new MemoryStream();
        using (var doc = PresentationDocument.Create(stream, PresentationDocumentType.Presentation, true))
        {
            var presentationPart = doc.AddPresentationPart();
            presentationPart.Presentation = new Presentation(
                new SlideIdList(),
                new SlideSize { Cx = 9144000, Cy = 6858000 },
                new NotesSize { Cx = 6858000, Cy = 9144000 }
            );

            var slidePart = presentationPart.AddNewPart<SlidePart>();
            var slideId = new SlideId { Id = 256, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
            presentationPart.Presentation.SlideIdList!.Append(slideId);

            // Build table
            var table = new D.Table();
            table.AppendChild(new D.TableProperties { FirstRow = true, BandRow = true });
            table.AppendChild(new D.TableGrid(
                new D.GridColumn { Width = 3000000 },
                new D.GridColumn { Width = 3000000 }
            ));

            // Row 0: Col 0 ("Header"), Col 1 (Merged continuation)
            var cell00 = CreateTableCell("Header");
            var cell01 = CreateTableCell("");
            cell01.HorizontalMerge = true;

            var row0 = new D.TableRow { Height = 370840 };
            row0.AppendChild(cell00);
            row0.AppendChild(cell01);
            table.AppendChild(row0);

            // Row 1: Col 0 ("A2"), Col 1 ("B2")
            var cell10 = CreateTableCell("A2");
            var cell11 = CreateTableCell("B2");

            var row1 = new D.TableRow { Height = 370840 };
            row1.AppendChild(cell10);
            row1.AppendChild(cell11);
            table.AppendChild(row1);

            var graphicData = new D.GraphicData(
                table
            ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/table" };

            var graphic = new D.Graphic(graphicData);

            var graphicFrame = new GraphicFrame(
                new NonVisualGraphicFrameProperties(
                    new NonVisualDrawingProperties { Id = 100, Name = "Table 1" },
                    new NonVisualGraphicFrameDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()
                ),
                new Transform(
                    new D.Offset { X = 500000, Y = 500000 },
                    new D.Extents { Cx = 6000000, Cy = 741680 }
                ),
                graphic
            );

            slidePart.Slide = new Slide(
                new CommonSlideData(new ShapeTree(
                    new NonVisualGroupShapeProperties(
                        new NonVisualDrawingProperties { Id = 1, Name = "" },
                        new NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()
                    ),
                    new GroupShapeProperties(new D.TransformGroup()),
                    graphicFrame
                ))
            );

            presentationPart.Presentation.Save();
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Saves a MemoryStream PPTX to a temp file and returns the path.
    /// Caller is responsible for deleting the file.
    /// </summary>
    public static string SaveToTempFile(MemoryStream pptxStream)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".pptx");
        pptxStream.Position = 0;
        using (var fs = File.Create(tempPath))
            pptxStream.CopyTo(fs);
        pptxStream.Position = 0;
        return tempPath;
    }
}
