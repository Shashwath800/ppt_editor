using DocumentFormat.OpenXml;
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
        // FIX: GetTempFileName() physically creates a 0-byte .tmp file on disk.
        // Appending ".pptx" produced a different path, leaking the .tmp file on every call.
        // GetRandomFileName() returns a name string only — no file is created.
        var tempFilePath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            System.IO.Path.GetRandomFileName() + ".pptx");

        // FIX: Declare memoryStream outside try so the catch can dispose it on failure.
        MemoryStream? memoryStream = null;
        try
        {
            System.IO.File.Copy(originalFilePath, tempFilePath, true);

            using (var presentationDocument = PresentationDocument.Open(tempFilePath, true))
            {
                var presentationPart = presentationDocument.PresentationPart;
                if (presentationPart?.Presentation?.SlideIdList == null)
                    throw new InvalidOperationException("Invalid PPTX: No presentation part or slide list.");

                var slideIds = presentationPart.Presentation.SlideIdList.Elements<P.SlideId>().ToList();

                foreach (var semanticSlide in presentation.Slides)
                {
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

                        var openXmlShapeId = element.Id.StartsWith("element_")
                            ? element.Id.Substring(8)
                            : element.Id;

                        var shape = slide.CommonSlideData.ShapeTree.Descendants<P.Shape>()
                            .FirstOrDefault(s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value.ToString() == openXmlShapeId);

                        if (shape?.TextBody == null)
                            continue;

                        var currentText = ExtractCurrentText(shape.TextBody);
                        var normalizedCurrent = NormalizeText(currentText);
                        var normalizedNew = NormalizeText(element.Text);

                        if (normalizedCurrent == normalizedNew)
                            continue;

                        UpdateShapeText(shape.TextBody, element.Text, element.Paragraphs);
                        slideModified = true;
                    }

                    if (slideModified)
                        slide.Save();
                }

                presentationPart.Presentation.Save();
            }

            memoryStream = new MemoryStream();
            using (var fileStream = System.IO.File.OpenRead(tempFilePath))
                await fileStream.CopyToAsync(memoryStream);

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch
        {
            // FIX: Dispose the stream if we fail after creating it but before returning.
            memoryStream?.Dispose();
            throw;
        }
        finally
        {
            // FIX: Temp file is always deleted — even when an exception is thrown mid-render.
            try { System.IO.File.Delete(tempFilePath); } catch { /* ignore cleanup errors */ }
        }
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

            // FIX: Iterate children in document order rather than runs and fields separately.
            // The previous approach silently skipped <a:br> (LineBreak) elements, causing
            // comparison mismatches on any shape containing in-paragraph soft line breaks —
            // those shapes were always flagged as changed and needlessly rewritten.
            foreach (var child in paragraph.ChildElements)
            {
                if (child is D.Run run)
                {
                    var text = run.GetFirstChild<D.Text>();
                    if (text != null)
                        sb.Append(text.Text);
                }
                else if (child is D.Field field)
                {
                    var text = field.GetFirstChild<D.Text>();
                    if (text != null)
                        sb.Append(text.Text);
                }
                else if (child is D.Break) // <a:br> — soft line break within a paragraph
                {
                    sb.Append('\n');
                }
            }

            if (i < paragraphs.Count - 1)
                sb.Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Normalizes text for comparison: trims and normalizes line endings.
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

        if (known.Contains(stored))
            return stored;

        if (!string.IsNullOrEmpty(stored))
        {
            var camel = char.ToLowerInvariant(stored[0]) + stored.Substring(1);
            if (known.Contains(camel))
                return camel;
        }

        return "arabicPeriod";
    }

    /// <summary>
    /// Inserts a bullet element at the correct position within ParagraphProperties.
    /// Per the CT_TextParagraphProperties schema, bullet elements (buNone/buChar/buAutoNum)
    /// must appear before tabLst, defRPr, and extLst. AppendChild would place them after
    /// defRPr, violating the schema and breaking strict-mode readers such as LibreOffice.
    /// </summary>
    private static void InsertBulletElement(D.ParagraphProperties pPr, OpenXmlElement bulletElement)
    {
        var anchor = pPr.ChildElements
            .FirstOrDefault(c => c.LocalName == "tabLst"
                              || c.LocalName == "defRPr"
                              || c.LocalName == "extLst");
        if (anchor != null)
            pPr.InsertBefore(bulletElement, anchor);
        else
            pPr.AppendChild(bulletElement);
    }

    /// <summary>
    /// Inserts a run (<a:r>) before <a:endParaRPr> if one exists, or appends it otherwise.
    ///
    /// FIX: The OOXML CT_TextParagraph schema requires runs to appear before
    /// <a:endParaRPr>. After stripping existing runs, <a:endParaRPr> remains as the
    /// last child of the paragraph. Using AppendChild() then places every new run
    /// after it, violating the schema. PowerPoint silently discards runs that follow
    /// <a:endParaRPr>, so the shape renders as blank even though the XML contains text.
    /// Inserting before <a:endParaRPr> restores the correct child order:
    ///   pPr → [runs] → endParaRPr
    /// </summary>
    private static void InsertRunBeforeEndParaRPr(D.Paragraph paragraph, D.Run run)
    {
        var endParaRPr = paragraph.GetFirstChild<D.EndParagraphRunProperties>();
        if (endParaRPr != null)
            paragraph.InsertBefore(run, endParaRPr);
        else
            paragraph.AppendChild(run);
    }

    /// <summary>
    /// Creates a <a:t> text element with xml:space="preserve" set unconditionally.
    ///
    /// FIX: The OpenXML SDK version used here does not expose a typed Space property on
    /// D.Text (DocumentFormat.OpenXml.Drawing.Text). Setting the attribute via
    /// SetAttribute is the version-agnostic approach and produces identical XML output.
    /// Without xml:space="preserve", the XML serialiser is free to collapse or strip
    /// whitespace at text-node boundaries — leading spaces, trailing spaces, and the
    /// spaces that sit between adjacent runs are all at risk. This causes words to run
    /// together whenever a formatted segment ends with a space character.
    /// </summary>
    private static D.Text CreatePreservedText(string text)
    {
        var t = new D.Text(text);
        t.SetAttribute(new OpenXmlAttribute("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve"));
        return t;
    }

    /// <summary>
    /// Updates text content while preserving original formatting as much as possible.
    /// Uses the SemanticElement's Paragraphs template to restore exact bullet types,
    /// numbering formats, and run-level styles (colors/fonts) per line.
    ///
    /// Key behavior: performs a proportional mapping between the new text words
    /// and the original template runs. Each word in the new text is assigned to
    /// a run based on its proportional position within the original template text.
    /// Whenever ANY formatting property differs between adjacent words' assigned
    /// runs, a new OpenXML run is created. This preserves multi-colored/multi-styled
    /// text and degrades gracefully when the AI significantly rewrites a line.
    /// </summary>
    private void UpdateShapeText(P.TextBody textBody, string newText, List<TextParagraph>? templateParagraphs)
    {
        var existingParagraphs = textBody.Elements<D.Paragraph>().ToList();

        var normalizedText = newText.Replace("\\n", "\n").Replace("\r\n", "\n").Replace("\r", "\n");
        var newLines = normalizedText.Split('\n');

        if (newLines.Length > existingParagraphs.Count && existingParagraphs.Count > 0)
        {
            var lastPara = existingParagraphs.Last();
            for (int i = existingParagraphs.Count; i < newLines.Length; i++)
            {
                var clone = (D.Paragraph)lastPara.CloneNode(true);
                // FIX: CloneNode copies <a:fld> id attributes verbatim. OOXML requires
                // field IDs to be unique within a file — duplicate IDs cause stale or
                // incorrect field rendering (slide numbers, dates, etc.).
                foreach (var field in clone.Descendants<D.Field>())
                    field.Id = "{" + Guid.NewGuid().ToString("D").ToUpper() + "}";
                textBody.AppendChild(clone);
                existingParagraphs.Add(clone);
            }
        }

        for (int i = 0; i < newLines.Length; i++)
        {
            var paragraph = existingParagraphs[i];

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

                if (tPara.IndentLevel > 0)
                    pPr.Level = tPara.IndentLevel;

                // FIX: Remove ALL bullet-related children, not just the three type nodes.
                // Previously, only buNone/buChar/buAutoNum were removed, leaving orphaned
                // <a:buClr>, <a:buSz*>, and <a:buFont*> children that referenced a bullet
                // type that no longer existed — producing a structurally invalid <pPr>.
                var bulletLocalNames = new HashSet<string>
                {
                    "buNone",  "buChar",   "buAutoNum", "buBlip",  // bullet type
                    "buClr",   "buClrTx",                           // bullet color
                    "buSzPct", "buSzPts",  "buSzTx",               // bullet size
                    "buFont",  "buFontTx"                           // bullet font
                };
                foreach (var child in pPr.ChildElements
                    .Where(c => bulletLocalNames.Contains(c.LocalName))
                    .ToList())
                {
                    child.Remove();
                }

                if (tPara.BulletType == "numbered" && tPara.NumberingFormat != null)
                {
                    var xmlValue = ToOoxmlAutoNumberString(tPara.NumberingFormat);
                    InsertBulletElement(pPr, new D.AutoNumberedBullet
                    {
                        Type = new D.TextAutoNumberSchemeValues(xmlValue)
                    });
                }
                else if (tPara.BulletType == "bullet")
                {
                    InsertBulletElement(pPr, new D.CharacterBullet { Char = "•" });
                }
                // FIX: Do NOT write <a:buNone/> when BulletType is null. Null means the
                // template recorded no preference — it does not mean "explicitly no bullet."
                // Writing buNone would override bullets inherited from the slide master/layout,
                // silently stripping theme bullets from every round-tripped paragraph.
            }

            // 2. Apply Run-Level Text and Styling — remove all existing runs and line breaks,
            //    then rebuild from scratch using the word-proportional segment map.
            // FIX: Also remove <a:br> (LineBreak) elements before rebuilding. Previously only
            // runs were removed; any <a:br> left behind would orphan after the new runs were
            // appended, producing out-of-order line break artifacts in the output.
            foreach (var existingRun in paragraph.Elements<D.Run>().ToList())
                existingRun.Remove();
            foreach (var lineBreak in paragraph.Elements<D.Break>().ToList())
                lineBreak.Remove();

            var lineText = newLines[i];

            if (tPara != null && tPara.Runs.Count > 0)
            {
                var templateRunMap = BuildCharacterRunMap(tPara.Runs);
                var segments = SplitNewTextIntoFormattedSegments(lineText, templateRunMap, tPara.Runs);

                foreach (var segment in segments)
                {
                    var r = new D.Run();
                    var rPr = BuildRunProperties(segment.TemplateRun);
                    r.AppendChild(rPr);
                    // FIX: Use CreatePreservedText so xml:space="preserve" is always set —
                    // without it the XML serialiser strips leading/trailing whitespace from
                    // text nodes, merging words at run boundaries (e.g. "Hello " + "World"
                    // becomes "HelloWorld"). D.Text does not expose a Space property in this
                    // SDK version, so the attribute is written via SetAttribute instead.
                    r.AppendChild(CreatePreservedText(segment.Text));
                    // FIX: Insert before <a:endParaRPr> — see InsertRunBeforeEndParaRPr.
                    InsertRunBeforeEndParaRPr(paragraph, r);
                }

                if (segments.Count == 0)
                {
                    // Empty line — add a placeholder run to preserve the paragraph's font.
                    var r = new D.Run();
                    r.AppendChild(BuildRunProperties(tPara.Runs[0]));
                    // FIX: CreatePreservedText and InsertRunBeforeEndParaRPr — same reasons above.
                    r.AppendChild(CreatePreservedText(""));
                    InsertRunBeforeEndParaRPr(paragraph, r);
                }
            }
            else
            {
                // No template — create a single run with default properties.
                var r = new D.Run();
                r.AppendChild(new D.RunProperties { Language = "en-US", Dirty = false });
                // FIX: CreatePreservedText and InsertRunBeforeEndParaRPr — same reasons above.
                r.AppendChild(CreatePreservedText(lineText));
                InsertRunBeforeEndParaRPr(paragraph, r);
            }
        }

        if (newLines.Length < existingParagraphs.Count)
        {
            for (int i = newLines.Length; i < existingParagraphs.Count; i++)
                existingParagraphs[i].Remove();
        }
    }

    /// <summary>
    /// Builds a map: character position (0-based) → index into templateRuns.
    /// For example, runs ["Hello ", "World"] produces [0,0,0,0,0,0,1,1,1,1,1].
    /// </summary>
    private List<int> BuildCharacterRunMap(List<TextRun> templateRuns)
    {
        var map = new List<int>();
        for (int runIdx = 0; runIdx < templateRuns.Count; runIdx++)
        {
            var text = templateRuns[runIdx].Text ?? "";
            for (int c = 0; c < text.Length; c++)
                map.Add(runIdx);
        }
        return map;
    }

    /// <summary>
    /// Splits text into whole-word tokens, each consisting of optional leading
    /// spaces followed by non-space characters.
    ///
    /// Example: "Hello World and more" → [("Hello", 0), (" World", 5), (" and", 11), (" more", 15)]
    ///
    /// Keeping spaces glued to the word that follows them ensures that every
    /// formatting boundary produced by SplitNewTextIntoFormattedSegments falls
    /// at a natural inter-word gap — never in the middle of a word.
    /// </summary>
    private static List<(string Text, int StartPos)> SplitIntoWordTokens(string text)
    {
        var tokens = new List<(string, int)>();
        int i = 0;
        while (i < text.Length)
        {
            int start = i;
            // Consume leading spaces (glued to the following word)
            while (i < text.Length && text[i] == ' ')
                i++;
            // Consume non-space characters (the actual word)
            while (i < text.Length && text[i] != ' ')
                i++;
            if (i > start)
                tokens.Add((text.Substring(start, i - start), start));
        }
        return tokens;
    }

    /// <summary>
    /// Splits the new line text into segments, each tagged with the TextRun whose
    /// formatting should apply.
    ///
    /// FIX: Switched from character-level to word-level proportional mapping.
    /// The previous character-level approach could assign different runs to adjacent
    /// characters of the same word whenever the new text was shorter than the template.
    ///
    /// Root cause: given template "Hello World..." where "Hello" (5 chars) is run 0
    /// (red) and " World..." (15 chars) is run 1 (black) — originalLength = 20 — and
    /// new text "Hello" (5 chars):
    ///
    ///   pos 0 'H': round(0/4 × 19) =  0 → run 0 ✓ red
    ///   pos 1 'e': round(1/4 × 19) =  5 → run 1 ✗ black  ← mid-word!
    ///   pos 2 'l': round(2/4 × 19) = 10 → run 1 ✗ black
    ///   pos 3 'l': round(3/4 × 19) = 14 → run 1 ✗ black
    ///   pos 4 'o': round(4/4 × 19) = 19 → run 1 ✗ black
    ///
    /// Only "H" rendered red; "ello" turned black — a partial mis-coloring of the
    /// first word.
    ///
    /// Fix: tokenize into whole words first via SplitIntoWordTokens, then map each
    /// WORD (not each character) proportionally to a run using the position of the
    /// word's first non-space character as the anchor. Because an entire word is
    /// always assigned to one run, formatting boundaries can only ever fall between
    /// words — never inside one.
    /// </summary>
    private List<FormattedSegment> SplitNewTextIntoFormattedSegments(
        string newText,
        List<int> charRunMap,
        List<TextRun> templateRuns)
    {
        var segments = new List<FormattedSegment>();
        if (string.IsNullOrEmpty(newText))
            return segments;

        var lastRunIdx = templateRuns.Count - 1;
        var originalLength = charRunMap.Count;
        var sb = new StringBuilder();
        int currentRunIdx = -1;

        // FIX: Operate on whole-word tokens so that formatting boundaries only
        // ever fall at spaces — never mid-word. Each token is (leading spaces +
        // word); its anchor for the proportional lookup is the position of the
        // first non-space character, i.e. the actual start of the word.
        var tokens = SplitIntoWordTokens(newText);

        foreach (var (tokenText, tokenStart) in tokens)
        {
            // Anchor the proportional lookup to the first non-space character of
            // the token (skipping any leading spaces). Using the word's true start
            // position gives the most accurate run assignment for that word.
            int anchorPos = tokenStart;
            while (anchorPos < tokenStart + tokenText.Length && newText[anchorPos] == ' ')
                anchorPos++;

            // Edge case: token consists entirely of spaces — fall back to the
            // token start so we still assign it to some run rather than throwing.
            if (anchorPos >= tokenStart + tokenText.Length)
                anchorPos = tokenStart;

            int mappedRunIdx;
            if (originalLength == 0)
            {
                mappedRunIdx = lastRunIdx;
            }
            else
            {
                // Map the word's anchor position proportionally onto the original
                // template length so that run-boundary percentages are preserved
                // regardless of how much the total text length has changed.
                var proportionalPos = (int)Math.Round(
                    (double)anchorPos / Math.Max(newText.Length - 1, 1) * (originalLength - 1));
                proportionalPos = Math.Clamp(proportionalPos, 0, originalLength - 1);
                mappedRunIdx = charRunMap[proportionalPos];
            }

            if (currentRunIdx < 0)
            {
                currentRunIdx = mappedRunIdx;
                sb.Append(tokenText);
            }
            else if (mappedRunIdx != currentRunIdx &&
                     !RunFormattingEqual(templateRuns[currentRunIdx], templateRuns[mappedRunIdx]))
            {
                segments.Add(new FormattedSegment
                {
                    Text = sb.ToString(),
                    TemplateRun = templateRuns[currentRunIdx]
                });
                sb.Clear();
                currentRunIdx = mappedRunIdx;
                sb.Append(tokenText);
            }
            else
            {
                sb.Append(tokenText);
            }
        }

        if (sb.Length > 0)
        {
            segments.Add(new FormattedSegment
            {
                Text = sb.ToString(),
                TemplateRun = templateRuns[currentRunIdx < 0 ? 0 : currentRunIdx]
            });
        }

        return segments;
    }

    /// <summary>
    /// Compares two TextRun instances for formatting equality across all properties.
    /// </summary>
    private bool RunFormattingEqual(TextRun a, TextRun b)
    {
        if (a.FontSize != b.FontSize) return false;
        if (a.Bold != b.Bold) return false;
        if (a.Italic != b.Italic) return false;
        if (!string.Equals(a.FontColor, b.FontColor, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(a.FontFamily, b.FontFamily, StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    /// <summary>
    /// Builds OpenXML RunProperties from a TextRun template, reproducing all stored
    /// formatting: font size, bold, italic, color, and font family.
    /// Child elements are appended in CT_TextCharacterProperties schema order:
    /// fill elements (solidFill) before font elements (latin), per the OOXML spec.
    /// </summary>
    private D.RunProperties BuildRunProperties(TextRun tRun)
    {
        var rPr = new D.RunProperties { Language = "en-US", Dirty = false };

        if (tRun.FontSize.HasValue)
            rPr.FontSize = (int)(tRun.FontSize.Value * 100);

        if (tRun.Bold)
            rPr.Bold = true;

        if (tRun.Italic)
            rPr.Italic = true;

        // solidFill must come before latin per CT_TextCharacterProperties schema order.
        if (!string.IsNullOrEmpty(tRun.FontColor) && tRun.FontColor.StartsWith("#"))
        {
            var hex = tRun.FontColor.Substring(1);
            rPr.AppendChild(new D.SolidFill(new D.RgbColorModelHex { Val = hex }));
        }

        if (!string.IsNullOrEmpty(tRun.FontFamily))
            rPr.AppendChild(new D.LatinFont { Typeface = tRun.FontFamily });

        return rPr;
    }

    /// <summary>
    /// Holds a text segment and the template run whose formatting applies to it.
    /// </summary>
    private class FormattedSegment
    {
        public string Text { get; set; } = string.Empty;
        public TextRun TemplateRun { get; set; } = new();
    }
}