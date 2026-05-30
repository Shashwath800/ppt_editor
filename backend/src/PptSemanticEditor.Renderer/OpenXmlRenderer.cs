using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace PptSemanticEditor.Renderer;

/// <summary>
/// Renders a SemanticPresentation to a PPTX file.
/// Deterministic — no AI involved.
/// </summary>
public class OpenXmlRenderer : IOpenXmlRenderer
{
    public async Task<Stream> RenderAsync(SemanticPresentation presentation, string originalFilePath)
    {
        var tempFilePath = System.IO.Path.GetTempFileName() + ".pptx";
        System.IO.File.Copy(originalFilePath, tempFilePath, true);

        using (var presentationDocument = PresentationDocument.Open(tempFilePath, true))
        {
            var presentationPart = presentationDocument.PresentationPart;
            if (presentationPart?.Presentation?.SlideIdList == null)
                throw new InvalidOperationException("Invalid PPTX: No presentation part or slide list.");

            var slideIds = presentationPart.Presentation.SlideIdList.Elements<P.SlideId>().ToList();

            // Iterate over the semantic slides
            foreach (var semanticSlide in presentation.Slides)
            {
                // Slide IDs are 1-based index (SlideIndex + 1)
                var slideIndex = semanticSlide.Id - 1;
                if (slideIndex < 0 || slideIndex >= slideIds.Count)
                    continue;

                var slideId = slideIds[slideIndex];
                var relationshipId = slideId.RelationshipId?.Value;
                if (string.IsNullOrEmpty(relationshipId))
                    continue;

                var slidePart = (SlidePart)presentationPart.GetPartById(relationshipId);
                var slide = slidePart.Slide;
                if (slide?.CommonSlideData?.ShapeTree == null)
                    continue;

                foreach (var element in semanticSlide.Elements)
                {
                    if (string.IsNullOrWhiteSpace(element.Text))
                        continue;

                    // Extract the raw OpenXML shape ID
                    var openXmlShapeId = element.Id.StartsWith("element_") 
                        ? element.Id.Substring(8) 
                        : element.Id;

                    // Find the shape in the XML tree
                    var shape = slide.CommonSlideData.ShapeTree.Descendants<P.Shape>()
                        .FirstOrDefault(s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value.ToString() == openXmlShapeId);

                    if (shape?.TextBody != null)
                    {
                        UpdateShapeText(shape.TextBody, element.Text);
                    }
                }

                slide.Save();
            }

            presentationPart.Presentation.Save();
        }

        // Read the modified file into a memory stream
        var memoryStream = new MemoryStream();
        using (var fileStream = System.IO.File.OpenRead(tempFilePath))
        {
            await fileStream.CopyToAsync(memoryStream);
        }
        
        try { System.IO.File.Delete(tempFilePath); } catch { /* Ignore cleanup errors */ }

        memoryStream.Position = 0;
        return memoryStream;
    }

    private void UpdateShapeText(P.TextBody textBody, string newText)
    {
        // Capture the original RunProperties from the first text run to preserve formatting
        var firstParagraph = textBody.Elements<D.Paragraph>().FirstOrDefault();
        var firstRun = firstParagraph?.Elements<D.Run>().FirstOrDefault();
        var originalRunProps = firstRun?.RunProperties?.CloneNode(true) as D.RunProperties;
        var originalParagraphProps = firstParagraph?.ParagraphProperties?.CloneNode(true) as D.ParagraphProperties;

        // Remove all existing paragraphs
        textBody.RemoveAllChildren<D.Paragraph>();

        // Split the new text into paragraphs
        var lines = newText.Split('\n');
        foreach (var line in lines)
        {
            var p = new D.Paragraph();

            if (originalParagraphProps != null)
                p.AppendChild(originalParagraphProps.CloneNode(true));

            var r = new D.Run();
            if (originalRunProps != null)
                r.AppendChild(originalRunProps.CloneNode(true));
            else
                r.AppendChild(new D.RunProperties { Language = "en-US", Dirty = false });

            r.AppendChild(new D.Text(line));
            p.AppendChild(r);

            textBody.AppendChild(p);
        }
    }
}
