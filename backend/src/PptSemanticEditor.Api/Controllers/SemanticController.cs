using Microsoft.AspNetCore.Mvc;
using PptSemanticEditor.Api.Services;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Api.Controllers;

[ApiController]
[Route("api/semantic")]
public class SemanticController : ControllerBase
{
    private readonly SessionStore _sessionStore;

    public SemanticController(SessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    [HttpGet("{sessionId}/tree")]
    public IActionResult GetSemanticTree(string sessionId)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        if (session.SemanticPresentation == null)
            return BadRequest("Semantic tree not yet available. Upload a file first.");

        return Ok(session.SemanticPresentation);
    }

    [HttpGet("{sessionId}/json")]
    public IActionResult GetSemanticJson(string sessionId)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        if (session.SemanticPresentation == null)
            return BadRequest("Semantic JSON not yet available. Upload a file first.");

        return Ok(session.SemanticPresentation);
    }

    [HttpPut("{sessionId}/json")]
    public IActionResult UpdateSemanticJson(string sessionId, [FromBody] SemanticPresentation updated)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        session.SemanticPresentation = updated;

        // Reset downstream stages since the JSON was manually edited
        session.PipelineState.SetStage("editPlan", "pending");
        session.PipelineState.SetStage("jsonTransformation", "pending");
        session.PipelineState.SetStage("rendering", "pending");
        session.PipelineState.SetStage("complete", "pending");
        session.AnalysisResult = null;
        session.ActionCommands = null;
        session.ModifiedPresentation = null;

        return Ok();
    }
}
