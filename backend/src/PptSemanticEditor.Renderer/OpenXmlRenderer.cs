using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;
using System.Text;

using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace PptSemanticEditor.Renderer;

/// <summary>
/// Renders a SemanticPresentation to a PPTX file.
/// Deterministic — no AI involved.
/// Only modifies shapes whose text content has actually changed.
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

                bool slideModified = false;

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

                    if (shape?.TextBody == null)
                        continue;

                    // Extract the current text from the shape to compare
                    var currentText = ExtractCurrentText(shape.TextBody);

                    // Normalize both texts for comparison (trim, normalize line endings)
                    var normalizedCurrent = NormalizeText(currentText);
                    var normalizedNew = NormalizeText(element.Text);

                    // Only update if text has actually changed
                    if (normalizedCurrent == normalizedNew)
                        continue;

                    // Only update text content — position/size/fill/font are already
                    // correct in the original PPTX copy. Writing them back would introduce
                    // rounding errors from the EMU → inches → EMU roundtrip.
                    UpdateShapeText(shape.TextBody, element.Text, element.Paragraphs);
                    slideModified = true;
                }

                // Only save the slide if we actually modified something
                if (slideModified)
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

    /// <summary>
    /// Extracts the current text content from a TextBody, matching the parser's extraction logic.
    /// </summary>
    private string ExtractCurrentText(P.TextBody textBody)
    {
        var sb = new StringBuilder();
        var paragraphs = textBody.Elements<D.Paragraph>().ToList();

        for (int i = 0; i < paragraphs.Count; i++)
        {
            var paragraph = paragraphs[i];
            foreach (var run in paragraph.Elements<D.Run>())
            {
                var text = run.GetFirstChild<D.Text>();
                if (text != null)
                    sb.Append(text.Text);
            }
            // Also check for fields (like slide numbers, dates)
            foreach (var field in paragraph.Elements<D.Field>())
            {
                var text = field.GetFirstChild<D.Text>();
                if (text != null)
                    sb.Append(text.Text);
            }
            // Add newline between paragraphs (but not after the last one)
            if (i < paragraphs.Count - 1)
                sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>
    /// Normalizes text for comparison: trims, normalizes line endings, collapses whitespace.
    /// </summary>
    private string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();
    }

    /// <summary>
    /// Maps a stored NumberingFormat string (either PascalCase C# enum name or
    /// camelCase XML value) to the exact OOXML camelCase string the SDK requires.
    /// The SDK validates against these exact strings during Save() — passing an
    /// unrecognized value throws "The specified value is not valid according to
    /// the specified enum type" at serialization time, not at construction time.
    /// </summary>
    private static string ToOoxmlAutoNumberString(string stored)
    {
        // Full set of valid camelCase XML attribute values from the OOXML spec
        var known = new HashSet<string>(StringComparer.Ordinal)
        {
            "alphaLcParenBoth", "alphaLcParenR",  "alphaLcPeriod",
            "alphaUcParenBoth", "alphaUcParenR",  "alphaUcPeriod",
            "arabicParenBoth",  "arabicParenR",   "arabicPeriod",
            "arabicPlain",
            "romanLcParenBoth", "romanLcParenR",  "romanLcPeriod",
            "romanUcParenBoth", "romanUcParenR",  "romanUcPeriod",
            "ea1ChsPeriod",     "ea1ChsPlain",
            "ea1ChtPeriod",     "ea1ChtPlain",
            "ea1JpnChsDbPeriod","ea1JpnKorPeriod","ea1JpnKorPlain",
            "hebrew2Minus",     "hindiAlpha1Period","hindiAlphaPeriod",
            "hindiNumParenR",   "hindiNumPeriod",
            "thai1Period",      "thai2Period",    "thaiAlphaPeriod",
        };

        // Already a valid camelCase XML value?
        if (known.Contains(stored))
            return stored;

        // Stored as PascalCase C# enum name (e.g. "ArabicPeriod") — convert to camelCase
        if (!string.IsNullOrEmpty(stored))
        {
            var camel = char.ToLowerInvariant(stored[0]) + stored.Substring(1);
            if (known.Contains(camel))
                return camel;
        }

        // Safe fallback
        return "arabicPeriod";
    }

    /// <summary>
    /// Updates text content while preserving original formatting as much as possible.
    /// Uses the SemanticElement's Paragraphs template to restore exact bullet types,
    /// numbering formats, and run-level styles (colors/fonts) per line.
    /// </summary>
    private void UpdateShapeText(P.TextBody textBody, string newText, List<TextParagraph>? templateParagraphs)
    {
        var existingParagraphs = textBody.Elements<D.Paragraph>().ToList();

        // Normalize line endings and unescape literal \n if the LLM generated them
        var normalizedText = newText.Replace("\\n", "\n").Replace("\r\n", "\n").Replace("\r", "\n");
        var newLines = normalizedText.Split('\n');

        // Ensure we have enough paragraphs in the text body
        if (newLines.Length > existingParagraphs.Count && existingParagraphs.Count > 0)
        {
            var lastPara = existingParagraphs.Last();
            for (int i = existingParagraphs.Count; i < newLines.Length; i++)
            {
                var clone = (D.Paragraph)lastPara.CloneNode(true);
                textBody.AppendChild(clone);
                existingParagraphs.Add(clone);
            }
        }

        // Update each line
        for (int i = 0; i < newLines.Length; i++)
        {
            var paragraph = existingParagraphs[i];
            var runs = paragraph.Elements<D.Run>().ToList();

            // Look up template paragraph if available (fallback to last available if we expanded)
            var templateIndex = templateParagraphs != null
                ? Math.Min(i, templateParagraphs.Count - 1)
                : -1;

            var tPara = templateIndex >= 0 ? templateParagraphs![templateIndex] : null;

            // 1. Apply Paragraph-Level Styling (Bullets/Numbering/Alignment)
            if (tPara != null)
            {
                var pPr = paragraph.GetFirstChild<D.ParagraphProperties>();
                if (pPr == null)
                {
                    pPr = new D.ParagraphProperties();
                    paragraph.InsertAt(pPr, 0);
                }

                // Restore alignment
                if (tPara.Alignment != null)
                {
                    pPr.Alignment = tPara.Alignment switch
                    {
                        "left"    => D.TextAlignmentTypeValues.Left,
                        "center"  => D.TextAlignmentTypeValues.Center,
                        "right"   => D.TextAlignmentTypeValues.Right,
                        "justify" => D.TextAlignmentTypeValues.Justified,
                        _         => pPr.Alignment
                    };
                }

                // Restore indent
                if (tPara.IndentLevel > 0)
                    pPr.Level = tPara.IndentLevel;

                // Remove any existing bullet/numbering definitions before re-applying
                pPr.RemoveAllChildren<D.AutoNumberedBullet>();
                pPr.RemoveAllChildren<D.CharacterBullet>();
                pPr.RemoveAllChildren<D.NoBullet>();

                if (tPara.BulletType == "numbered" && tPara.NumberingFormat != null)
                {
                    // FIXED: ToOoxmlAutoNumberString guarantees a valid camelCase XML string.
                    // Passing an invalid/PascalCase string to TextAutoNumberSchemeValues does NOT
                    // throw at construction time — it throws later inside slide.Save() during
                    // XML serialization, producing the 500 "not valid according to enum type" error.
                    var xmlValue = ToOoxmlAutoNumberString(tPara.NumberingFormat);
                    pPr.AppendChild(new D.AutoNumberedBullet
                    {
                        Type = new D.TextAutoNumberSchemeValues(xmlValue)
                    });
                }
                else if (tPara.BulletType == "bullet")
                {
                    pPr.AppendChild(new D.CharacterBullet { Char = "•" });
                }
                else if (tPara.BulletType == null)
                {
                    pPr.AppendChild(new D.NoBullet());
                }
            }

            // 2. Apply Run-Level Text and Styling
            if (runs.Count > 0)
            {
                // Put all the new text into the first run
                var firstRunText = runs[0].GetFirstChild<D.Text>();
                if (firstRunText != null)
                    firstRunText.Text = newLines[i];
                else
                    runs[0].AppendChild(new D.Text(newLines[i]));

                // Apply template styling to the first run
                if (tPara != null && tPara.Runs.Count > 0)
                {
                    var tRun = tPara.Runs[0]; // Usually the first run dictates the style
                    var rPr = runs[0].RunProperties;
                    if (rPr == null)
                    {
                        rPr = new D.RunProperties { Language = "en-US", Dirty = false };
                        runs[0].InsertBefore(rPr, runs[0].GetFirstChild<D.Text>());
                    }

                    if (tRun.FontSize.HasValue)
                        rPr.FontSize = (int)(tRun.FontSize.Value * 100);

                    if (tRun.Bold) rPr.Bold = true;
                    if (tRun.Italic) rPr.Italic = true;

                    // Reconstruct color if we have it
                    if (!string.IsNullOrEmpty(tRun.FontColor) && tRun.FontColor.StartsWith("#"))
                    {
                        var hex = tRun.FontColor.Substring(1);
                        var solidFill = rPr.GetFirstChild<D.SolidFill>();
                        if (solidFill == null)
                        {
                            solidFill = new D.SolidFill();
                            rPr.AppendChild(solidFill);
                        }

                        var rgbColor = solidFill.GetFirstChild<D.RgbColorModelHex>();
                        if (rgbColor == null)
                        {
                            rgbColor = new D.RgbColorModelHex();
                            solidFill.AppendChild(rgbColor);
                        }
                        rgbColor.Val = hex;
                    }
                }

                // Physically remove all subsequent runs instead of just emptying them
                for (int j = 1; j < runs.Count; j++)
                {
                    runs[j].Remove();
                }
            }
            else
            {
                // No runs in this paragraph — add one with default/template properties
                var r = new D.Run();
                var rPr = new D.RunProperties { Language = "en-US", Dirty = false };

                if (tPara != null && tPara.Runs.Count > 0)
                {
                    var tRun = tPara.Runs[0];
                    if (tRun.FontSize.HasValue) rPr.FontSize = (int)(tRun.FontSize.Value * 100);
                    if (tRun.Bold) rPr.Bold = true;
                    if (tRun.Italic) rPr.Italic = true;
                    if (!string.IsNullOrEmpty(tRun.FontColor) && tRun.FontColor.StartsWith("#"))
                    {
                        rPr.AppendChild(new D.SolidFill(new D.RgbColorModelHex { Val = tRun.FontColor.Substring(1) }));
                    }
                }

                r.AppendChild(rPr);
                r.AppendChild(new D.Text(newLines[i]));
                paragraph.AppendChild(r);
            }
        }

        // If we have fewer new lines than existing paragraphs, remove the excess paragraphs
        if (newLines.Length < existingParagraphs.Count)
        {
            for (int i = newLines.Length; i < existingParagraphs.Count; i++)
            {
                existingParagraphs[i].Remove();
            }
        }
    }
}