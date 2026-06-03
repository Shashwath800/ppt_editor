using Microsoft.AspNetCore.Mvc;
using PptSemanticEditor.Api.Services;
using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Api.Controllers;

[ApiController]
[Route("api/validation")]
public class ValidationController : ControllerBase
{
    private readonly SessionStore _sessionStore;
    private readonly IValidationEngine _validationEngine;

    public ValidationController(SessionStore sessionStore, IValidationEngine validationEngine)
    {
        _sessionStore = sessionStore;
        _validationEngine = validationEngine;
    }

    [HttpPost("{sessionId}/validate")]
    public IActionResult Validate(string sessionId)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        var targetPresentation = session.ModifiedPresentation ?? session.SemanticPresentation;
        if (targetPresentation == null)
            return BadRequest("No semantic presentation available to validate.");

        try
        {
            var result = _validationEngine.Validate(targetPresentation);
            session.LastValidation = result;
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Validation failed: {ex.Message}");
        }
    }

    [HttpGet("{sessionId}/history")]
    public IActionResult GetHistory(string sessionId)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        return Ok(session.VersionHistory);
    }

    [HttpPost("{sessionId}/rollback/{version}")]
    public IActionResult Rollback(string sessionId, int version)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        var snapshot = session.VersionHistory.GetVersion(version);
        if (snapshot == null)
            return NotFound($"Version {version} not found in history.");

        // Deep-clone the snapshot so mutations don't leak into version history
        var clonedPresentation = session.VersionHistory.GetSnapshotClone(version);
        if (clonedPresentation == null)
            return StatusCode(500, "Failed to clone version snapshot.");

        // Restore the snapshot as both modified and base presentation
        // so subsequent edits use the rolled-back state as their starting point
        session.ModifiedPresentation = clonedPresentation;
        session.SemanticPresentation = session.VersionHistory.GetSnapshotClone(version)!;

        // Reset downstream pipeline stages since the presentation has changed
        session.PipelineState.SetStage("editPlan", "pending");
        session.PipelineState.SetStage("jsonTransformation", "pending");
        session.PipelineState.SetStage("rendering", "pending");
        session.PipelineState.SetStage("complete", "pending");
        session.RenderedFilePath = null;
        session.ActionCommands = null;

        // Add a new version entry for the rollback
        session.VersionHistory.AddVersion(clonedPresentation, $"Rollback to version {version}");

        return Ok(session.ModifiedPresentation);
    }
}
