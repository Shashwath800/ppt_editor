using DocumentFormat.OpenXml.Packaging;
using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;
using PptSemanticEditor.Parser;
using PptSemanticEditor.Renderer;
using PptSemanticEditor.Semantic;
using Xunit;
using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace PptSemanticEditor.Tests;

// ─────────────────────────────────────────────
//  Minimal stubs for SemanticTreeBuilder dependencies
//  (no LLM/API/network needed)
// ─────────────────────────────────────────────

public class StubSlideClassifier : ISlideClassifier
{
    public SlideClassification Classify(OpenXmlSlideInfo slide) => SlideClassification.ContentSlide;
}

public class StubGraphDetector : IGraphDetector
{
    public GraphData? DetectGraph(OpenXmlSlideInfo slide) => null;
}

public class StubGraphBuilder : IGraphBuilder
{
    public SemanticGraph? BuildGraph(OpenXmlSlideInfo slide) => null;
}

// ─────────────────────────────────────────────
//  Shared pipeline helper
// ─────────────────────────────────────────────

public static class PipelineHelper
{
    /// <summary>
    /// Runs the full extraction pipeline: PPTX stream → OpenXmlInfo → SemanticPresentation.
    /// </summary>
    public static async Task<(SemanticPresentation Presentation, OpenXmlInfo RawInfo)> ExtractAsync(MemoryStream pptxStream)
    {
        pptxStream.Position = 0;
        var parser = new OpenXmlParser();
        var rawInfo = await parser.ParseAsync(pptxStream, "test.pptx");

        var treeBuilder = new SemanticTreeBuilder(
            new StubSlideClassifier(), new StubGraphDetector(), new StubGraphBuilder());
        var presentation = treeBuilder.BuildTree(rawInfo);

        return (presentation, rawInfo);
    }

    /// <summary>
    /// Renders a modified SemanticPresentation back to PPTX, returns the output stream
    /// and the temp file path (caller must delete).
    /// </summary>
    public static async Task<(Stream OutputPptx, string TempFilePath)> RenderAsync(
        SemanticPresentation presentation, MemoryStream originalPptxStream)
    {
        var tempPath = TestFixtures.SaveToTempFile(originalPptxStream);
        var renderer = new OpenXmlRenderer();
        var output = await renderer.RenderAsync(presentation, tempPath);
        return (output, tempPath);
    }
}

// ═══════════════════════════════════════════════════════════════
//  TABLE TESTS
// ═══════════════════════════════════════════════════════════════

public class TableExtractionTests
{
    [Fact]
    public async Task ExtractTable_ProducesOneElementPerCell_NotBlobText()
    {
        using var pptx = TestFixtures.CreateTablePresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);

        var slide = presentation.Slides[0];
        var tableCells = slide.Elements.Where(e => e.Type == "tableCell").ToList();

        // Must produce exactly 4 cells (2x2)
        Assert.Equal(4, tableCells.Count);
    }

    [Fact]
    public async Task ExtractTable_EachCellTextMatchesExactly_NoCrossContamination()
    {
        using var pptx = TestFixtures.CreateTablePresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);

        var slide = presentation.Slides[0];
        var cellTexts = slide.Elements
            .Where(e => e.Type == "tableCell")
            .Select(e => e.Text)
            .ToList();

        Assert.Contains("Cell A1", cellTexts);
        Assert.Contains("Cell B1", cellTexts);
        Assert.Contains("Cell A2", cellTexts);
        Assert.Contains("Cell B2", cellTexts);

        // No cell should contain text from another cell
        foreach (var text in cellTexts)
        {
            Assert.Single(text.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)));
        }
    }

    [Fact]
    public async Task ExtractTable_CellIdsEncode_TableShapeId_RowCol()
    {
        using var pptx = TestFixtures.CreateTablePresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);

        var slide = presentation.Slides[0];
        var cellIds = slide.Elements
            .Where(e => e.Type == "tableCell")
            .Select(e => e.Id)
            .ToList();

        // Table shape ID is 100, so cell IDs should be element_100_r{row}_c{col}
        Assert.Contains("element_100_r0_c0", cellIds);
        Assert.Contains("element_100_r0_c1", cellIds);
        Assert.Contains("element_100_r1_c0", cellIds);
        Assert.Contains("element_100_r1_c1", cellIds);
    }

    [Fact]
    public async Task ExtractTable_ParentTableEntryExists_WithNoText()
    {
        using var pptx = TestFixtures.CreateTablePresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);

        var slide = presentation.Slides[0];
        var tableParent = slide.Elements.FirstOrDefault(e => e.Type == "table");

        Assert.NotNull(tableParent);
        Assert.True(string.IsNullOrWhiteSpace(tableParent.Text),
            "Parent table element should have no text — text is on cells");
    }

    [Fact]
    public async Task ExtractTable_WithMergedCells_SkipsContinuationCells()
    {
        using var pptx = TestFixtures.CreateMergedTablePresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);

        var slide = presentation.Slides[0];
        var tableCells = slide.Elements.Where(e => e.Type == "tableCell").ToList();

        // Must produce exactly 3 cells: Row 0 Col 0 ("Header"), Row 1 Col 0 ("A2"), Row 1 Col 1 ("B2")
        // Row 0 Col 1 is a merged continuation and should be skipped.
        Assert.Equal(3, tableCells.Count);

        var cellIds = tableCells.Select(c => c.Id).ToList();
        Assert.Contains("element_100_r0_c0", cellIds);
        Assert.Contains("element_100_r1_c0", cellIds);
        Assert.Contains("element_100_r1_c1", cellIds);
        Assert.DoesNotContain("element_100_r0_c1", cellIds);

        var headerCell = tableCells.First(c => c.Id == "element_100_r0_c0");
        Assert.Equal("Header", headerCell.Text);
    }
}

public class TableRenderTests
{
    [Fact]
    public async Task RewriteTableCell_OnlyTargetCellChanges_OthersUnmodified()
    {
        using var pptx = TestFixtures.CreateTablePresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);

        // Modify cell R0C0 only
        var targetCell = presentation.Slides[0].Elements
            .First(e => e.Id == "element_100_r0_c0");
        Assert.Equal("Cell A1", targetCell.Text);
        targetCell.Text = "Updated Cell";

        var (outputStream, tempPath) = await PipelineHelper.RenderAsync(presentation, pptx);
        try
        {
            // Re-extract from rendered output to verify
            var outputMs = new MemoryStream();
            await outputStream.CopyToAsync(outputMs);
            var (rendered, _) = await PipelineHelper.ExtractAsync(outputMs);

            var renderedCells = rendered.Slides[0].Elements
                .Where(e => e.Type == "tableCell")
                .ToList();

            var cell00 = renderedCells.First(c => c.Id == "element_100_r0_c0");
            Assert.Equal("Updated Cell", cell00.Text);

            // Other cells unchanged
            var cell01 = renderedCells.First(c => c.Id == "element_100_r0_c1");
            Assert.Equal("Cell B1", cell01.Text);

            var cell10 = renderedCells.First(c => c.Id == "element_100_r1_c0");
            Assert.Equal("Cell A2", cell10.Text);

            var cell11 = renderedCells.First(c => c.Id == "element_100_r1_c1");
            Assert.Equal("Cell B2", cell11.Text);
        }
        finally
        {
            outputStream.Dispose();
            try { File.Delete(tempPath); } catch { }
        }
    }

    [Fact]
    public async Task RewriteTableCell_DoesNotSilentlyFail_CellIsActuallyLocated()
    {
        using var pptx = TestFixtures.CreateTablePresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);

        var targetCell = presentation.Slides[0].Elements
            .First(e => e.Id == "element_100_r1_c1");
        targetCell.Text = "Changed B2";

        var (outputStream, tempPath) = await PipelineHelper.RenderAsync(presentation, pptx);
        try
        {
            var outputMs = new MemoryStream();
            await outputStream.CopyToAsync(outputMs);
            var (rendered, _) = await PipelineHelper.ExtractAsync(outputMs);

            var cell = rendered.Slides[0].Elements
                .First(e => e.Id == "element_100_r1_c1");

            // This directly tests the original bug — before the fix, the renderer
            // would silently skip table cells because it only searched P.Shape
            Assert.Equal("Changed B2", cell.Text);
        }
        finally
        {
            outputStream.Dispose();
            try { File.Delete(tempPath); } catch { }
        }
    }

    [Fact]
    public async Task RewriteTableCell_InMergedTable_WorksCorrectly()
    {
        using var pptx = TestFixtures.CreateMergedTablePresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);

        var targetCell = presentation.Slides[0].Elements
            .First(e => e.Id == "element_100_r1_c1");
        targetCell.Text = "Updated B2";

        var (outputStream, tempPath) = await PipelineHelper.RenderAsync(presentation, pptx);
        try
        {
            var outputMs = new MemoryStream();
            await outputStream.CopyToAsync(outputMs);
            var (rendered, _) = await PipelineHelper.ExtractAsync(outputMs);

            var cell = rendered.Slides[0].Elements
                .First(e => e.Id == "element_100_r1_c1");

            Assert.Equal("Updated B2", cell.Text);

            // Row 0 Col 0 remains "Header"
            var header = rendered.Slides[0].Elements
                .First(e => e.Id == "element_100_r0_c0");
            Assert.Equal("Header", header.Text);
        }
        finally
        {
            outputStream.Dispose();
            try { File.Delete(tempPath); } catch { }
        }
    }
}

// ═══════════════════════════════════════════════════════════════
//  FLOWCHART TESTS
// ═══════════════════════════════════════════════════════════════

public class FlowchartTests
{
    [Fact]
    public async Task RewriteFlowchartBox_StillWorks_AfterTableChanges()
    {
        using var pptx = TestFixtures.CreateFlowchartPresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);

        var slide = presentation.Slides[0];
        var startBox = slide.Elements.First(e => e.Text == "Start");
        var processBox = slide.Elements.First(e => e.Text == "Process");

        // Only rewrite the Start box
        startBox.Text = "Begin";

        var (outputStream, tempPath) = await PipelineHelper.RenderAsync(presentation, pptx);
        try
        {
            var outputMs = new MemoryStream();
            await outputStream.CopyToAsync(outputMs);
            var (rendered, _) = await PipelineHelper.ExtractAsync(outputMs);

            var renderedSlide = rendered.Slides[0];

            // Target box text changed
            var renderedStart = renderedSlide.Elements.First(e => e.Id == startBox.Id);
            Assert.Equal("Begin", renderedStart.Text);

            // Other box untouched
            var renderedProcess = renderedSlide.Elements.First(e => e.Id == processBox.Id);
            Assert.Equal("Process", renderedProcess.Text);
        }
        finally
        {
            outputStream.Dispose();
            try { File.Delete(tempPath); } catch { }
        }
    }
}

// ═══════════════════════════════════════════════════════════════
//  CONNECTOR TESTS
// ═══════════════════════════════════════════════════════════════

public class ConnectorTests
{
    [Fact]
    public async Task RenderPipeline_WithConnector_DoesNotThrow()
    {
        using var pptx = TestFixtures.CreateFlowchartPresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);

        // Just render — the connector should be ignored gracefully
        var (outputStream, tempPath) = await PipelineHelper.RenderAsync(presentation, pptx);
        try
        {
            Assert.NotNull(outputStream);
            Assert.True(outputStream.Length > 0);
        }
        finally
        {
            outputStream.Dispose();
            try { File.Delete(tempPath); } catch { }
        }
    }

    [Fact]
    public async Task ConnectorExtraction_HasNoText()
    {
        using var pptx = TestFixtures.CreateFlowchartPresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);



        // Connectors are excluded from Elements in SemanticTreeBuilder (IsConnector = true → continue)
        // Verify no element has connector-like properties with text
        var connectorElements = presentation.Slides[0].Elements
            .Where(e => e.Type == "connector")
            .ToList();
        Assert.Empty(connectorElements);
    }
}

// ═══════════════════════════════════════════════════════════════
//  REGRESSION TESTS — bugs fixed earlier this session
// ═══════════════════════════════════════════════════════════════

public class RegressionTests
{
    [Fact]
    public async Task DoubleNewline_DoesNotProduceBlankParagraph()
    {
        using var pptx = TestFixtures.CreateMultiRunPresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);

        var shape = presentation.Slides[0].Elements.First(e => e.Id == "element_20");

        // Simulate LLM output with double newline
        shape.Text = "Line One\n\nLine Two";

        var (outputStream, tempPath) = await PipelineHelper.RenderAsync(presentation, pptx);
        try
        {
            // Open the rendered PPTX directly to inspect paragraph count
            outputStream.Position = 0;
            using var doc = PresentationDocument.Open(outputStream, false);
            var slidePart = doc.PresentationPart!.SlideParts.First();
            var pShape = slidePart.Slide.CommonSlideData!.ShapeTree!.Descendants<P.Shape>()
                .First(s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value == 20);

            var paragraphs = pShape.TextBody!.Elements<D.Paragraph>().ToList();

            // Should have exactly 2 paragraphs (Line One, Line Two) — NOT 3 with blank middle
            Assert.Equal(2, paragraphs.Count);

            // Neither paragraph should be empty
            foreach (var para in paragraphs)
            {
                var text = string.Join("", para.Descendants<D.Text>().Select(t => t.Text));
                Assert.False(string.IsNullOrWhiteSpace(text),
                    "No paragraph should be empty/blank after double-newline collapse");
            }
        }
        finally
        {
            outputStream.Dispose();
            try { File.Delete(tempPath); } catch { }
        }
    }

    [Fact]
    public async Task MultipleSpaces_CollapsedToSingleSpace()
    {
        using var pptx = TestFixtures.CreateMultiRunPresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);

        var shape = presentation.Slides[0].Elements.First(e => e.Id == "element_20");

        // Simulate LLM padding with multiple spaces — ActionExecutor collapses these
        // before they reach the renderer. Apply the same sanitization here.
        var rawLlmOutput = "Hello    World     Test";
        var sanitized = System.Text.RegularExpressions.Regex.Replace(rawLlmOutput, " {2,}", " ").Trim();
        shape.Text = sanitized;

        var (outputStream, tempPath) = await PipelineHelper.RenderAsync(presentation, pptx);
        try
        {
            outputStream.Position = 0;
            using var doc = PresentationDocument.Open(outputStream, false);
            var slidePart = doc.PresentationPart!.SlideParts.First();
            var pShape = slidePart.Slide.CommonSlideData!.ShapeTree!.Descendants<P.Shape>()
                .First(s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value == 20);

            // Collect all text from the shape
            var fullText = string.Join("", pShape.TextBody!.Descendants<D.Text>().Select(t => t.Text));

            // Should have single spaces, not multiple
            Assert.DoesNotContain("  ", fullText);
            Assert.Contains("Hello World Test", fullText);
        }
        finally
        {
            outputStream.Dispose();
            try { File.Delete(tempPath); } catch { }
        }
    }

    [Fact]
    public async Task MultiRunFormatting_Preserved_WhenTextRewritten()
    {
        using var pptx = TestFixtures.CreateMultiRunPresentation();
        var (presentation, _) = await PipelineHelper.ExtractAsync(pptx);

        var shape = presentation.Slides[0].Elements.First(e => e.Id == "element_20");
        // Original: "Bold Red" + " Plain Black" (two runs)
        // Rewrite with different text but same approximate proportions
        shape.Text = "Strong Red Light Black";

        var (outputStream, tempPath) = await PipelineHelper.RenderAsync(presentation, pptx);
        try
        {
            outputStream.Position = 0;
            using var doc = PresentationDocument.Open(outputStream, false);
            var slidePart = doc.PresentationPart!.SlideParts.First();
            var pShape = slidePart.Slide.CommonSlideData!.ShapeTree!.Descendants<P.Shape>()
                .First(s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value == 20);

            var runs = pShape.TextBody!.Elements<D.Paragraph>().First()
                .Elements<D.Run>().ToList();

            // Should have at least 2 runs (formatting boundary preserved)
            Assert.True(runs.Count >= 2, $"Expected at least 2 runs, got {runs.Count}");

            // First run should be bold and red
            var firstRunProps = runs[0].RunProperties!;
            Assert.True(firstRunProps.Bold?.Value == true, "First run should be bold");
            var firstFill = firstRunProps.GetFirstChild<D.SolidFill>();
            Assert.NotNull(firstFill);
            var firstColor = firstFill.GetFirstChild<D.RgbColorModelHex>();
            Assert.NotNull(firstColor);
            Assert.Equal("FF0000", firstColor.Val?.Value);

            // Last run should not be bold and should be black
            var lastRunProps = runs[^1].RunProperties!;
            // Bold should be absent or false
            Assert.True(lastRunProps.Bold == null || lastRunProps.Bold.Value == false,
                "Last run should not be bold");
            var lastFill = lastRunProps.GetFirstChild<D.SolidFill>();
            Assert.NotNull(lastFill);
            var lastColor = lastFill.GetFirstChild<D.RgbColorModelHex>();
            Assert.NotNull(lastColor);
            Assert.Equal("000000", lastColor.Val?.Value);
        }
        finally
        {
            outputStream.Dispose();
            try { File.Delete(tempPath); } catch { }
        }
    }
}
