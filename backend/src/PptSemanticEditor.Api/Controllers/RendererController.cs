using Microsoft.AspNetCore.Mvc;
using PptSemanticEditor.Api.Services;
using PptSemanticEditor.Core.Interfaces;

namespace PptSemanticEditor.Api.Controllers;

[ApiController]
[Route("api/renderer")]
public class RendererController : ControllerBase
{
    private readonly SessionStore _sessionStore;
    private readonly IOpenXmlRenderer _renderer;
    private readonly StorageSettings _storageSettings;

    public RendererController(
        SessionStore sessionStore,
        IOpenXmlRenderer renderer,
        StorageSettings storageSettings)
    {
        _sessionStore = sessionStore;
        _renderer = renderer;
        _storageSettings = storageSettings;
    }

    [HttpPost("{sessionId}/render")]
    public async Task<IActionResult> Render(string sessionId)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        // Use modified presentation if available, otherwise use original
        var presentation = session.ModifiedPresentation ?? session.SemanticPresentation;
        if (presentation == null)
            return BadRequest("No presentation data available for rendering.");

        if (string.IsNullOrEmpty(session.FilePath) || !System.IO.File.Exists(session.FilePath))
            return BadRequest("Original PPTX file not found on disk. Please re-upload.");

        session.PipelineState.SetStage("rendering", "inProgress");

        try
        {
            using var pptxStream = await _renderer.RenderAsync(presentation, session.FilePath);

            // Save to uploads directory
            var outputPath = Path.Combine(_storageSettings.Path, $"{sessionId}_output.pptx");

            using (var fileStream = new FileStream(outputPath, FileMode.Create))
            {
                await pptxStream.CopyToAsync(fileStream);
            }

            session.RenderedFilePath = outputPath;
            session.PipelineState.CompleteStage("rendering");
            session.PipelineState.CompleteStage("complete");

            return Ok(new { message = "Rendering complete", path = outputPath });
        }
        catch (Exception ex)
        {
            session.PipelineState.FailStage("rendering");
            return StatusCode(500, $"Rendering failed: {ex.Message}");
        }
    }

    [HttpGet("{sessionId}/download")]
    public IActionResult Download(string sessionId)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        if (string.IsNullOrEmpty(session.RenderedFilePath) || !System.IO.File.Exists(session.RenderedFilePath))
            return BadRequest("No rendered file available. Run /render first.");

        var fileName = $"modified_{session.FileName}";
        var fileStream = new FileStream(session.RenderedFilePath, FileMode.Open, FileAccess.Read);
        return File(fileStream, "application/vnd.openxmlformats-officedocument.presentationml.presentation", fileName);
    }
}
