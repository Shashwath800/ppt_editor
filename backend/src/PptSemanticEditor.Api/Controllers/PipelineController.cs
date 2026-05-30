using Microsoft.AspNetCore.Mvc;
using PptSemanticEditor.Api.Services;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Api.Controllers;

[ApiController]
[Route("api/pipeline")]
public class PipelineController : ControllerBase
{
    private readonly SessionStore _sessionStore;

    public PipelineController(SessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    [HttpGet("{sessionId}")]
    public IActionResult GetPipelineState(string sessionId)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        return Ok(session.PipelineState);
    }

    [HttpGet("{sessionId}/diff")]
    public IActionResult GetDiff(string sessionId)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        if (session.SemanticPresentation == null)
            return BadRequest("Original presentation not available.");

        var diff = new DiffResult
        {
            Original = session.SemanticPresentation,
            Modified = session.ModifiedPresentation ?? session.SemanticPresentation
        };

        return Ok(diff);
    }
}
