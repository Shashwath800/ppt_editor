using Microsoft.AspNetCore.Mvc;
using PptSemanticEditor.Api.Services;
using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Semantic;

namespace PptSemanticEditor.Api.Controllers;

[ApiController]
[Route("api/upload")]
public class UploadController : ControllerBase
{
    private readonly SessionStore _sessionStore;
    private readonly IOpenXmlParser _parser;
    private readonly IAstBuilder _astBuilder;
    private readonly ISemanticTreeBuilder _treeBuilder;
    private readonly JsonGenerator _jsonGenerator;
    private readonly StorageSettings _storageSettings;

    public UploadController(
        SessionStore sessionStore,
        IOpenXmlParser parser,
        IAstBuilder astBuilder,
        ISemanticTreeBuilder treeBuilder,
        JsonGenerator jsonGenerator,
        StorageSettings storageSettings)
    {
        _sessionStore = sessionStore;
        _parser = parser;
        _astBuilder = astBuilder;
        _treeBuilder = treeBuilder;
        _jsonGenerator = jsonGenerator;
        _storageSettings = storageSettings;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (!file.FileName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .pptx files are supported.");

        // Create session
        var session = _sessionStore.CreateSession(file.FileName);
        session.PipelineState.SetStage("upload", "inProgress");

        try
        {
            // Save uploaded file
            var filePath = Path.Combine(_storageSettings.Path, $"{session.SessionId}_{file.FileName}");

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            session.FilePath = filePath;
            session.PipelineState.CompleteStage("upload");

            // Auto-parse: OpenXML parsing
            session.PipelineState.SetStage("openXmlParsing", "inProgress");
            using var parseStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            session.OpenXmlInfo = await _parser.ParseAsync(parseStream, file.FileName);
            session.PipelineState.CompleteStage("openXmlParsing");

            // Auto-build: AST
            session.PipelineState.SetStage("astBuilding", "inProgress");
            session.Ast = _astBuilder.BuildAst(session.OpenXmlInfo);
            session.PipelineState.CompleteStage("astBuilding");

            // Auto-build: Semantic tree and graphs
            session.PipelineState.SetStage("semanticTree", "inProgress");
            session.PipelineState.SetStage("semanticGraph", "inProgress");
            // The tree builder now also builds graphs internally
            session.SemanticPresentation = _treeBuilder.BuildTree(session.OpenXmlInfo);
            session.PipelineState.CompleteStage("semanticTree");
            session.PipelineState.CompleteStage("semanticGraph");

            // Initialize version history with original
            session.VersionHistory.AddVersion(session.SemanticPresentation, "Original Upload");

            // Auto-generate: Semantic JSON
            session.PipelineState.SetStage("semanticJson", "inProgress");
            // JSON is generated on-demand from SemanticPresentation, so just mark complete
            session.PipelineState.CompleteStage("semanticJson");

            return Ok(session.PipelineState);
        }
        catch (Exception ex)
        {
            session.PipelineState.FailStage(session.PipelineState.CurrentStage);
            return StatusCode(500, $"Processing failed: {ex.Message}");
        }
    }

    [HttpGet("{sessionId}/status")]
    public IActionResult GetStatus(string sessionId)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        return Ok(session.PipelineState);
    }
}
