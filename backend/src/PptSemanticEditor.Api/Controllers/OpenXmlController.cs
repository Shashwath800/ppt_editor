using Microsoft.AspNetCore.Mvc;
using PptSemanticEditor.Api.Services;

namespace PptSemanticEditor.Api.Controllers;

[ApiController]
[Route("api/openxml")]
public class OpenXmlController : ControllerBase
{
    private readonly SessionStore _sessionStore;

    public OpenXmlController(SessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    [HttpGet("{sessionId}")]
    public IActionResult GetOpenXmlInfo(string sessionId)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        if (session.OpenXmlInfo == null)
            return BadRequest("OpenXML data not yet available. Upload a file first.");

        return Ok(session.OpenXmlInfo);
    }

    [HttpGet("{sessionId}/slide/{slideIndex:int}")]
    public IActionResult GetSlide(string sessionId, int slideIndex)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        if (session.OpenXmlInfo == null)
            return BadRequest("OpenXML data not yet available.");

        if (slideIndex < 0 || slideIndex >= session.OpenXmlInfo.Slides.Count)
            return BadRequest($"Slide index {slideIndex} out of range (0-{session.OpenXmlInfo.Slides.Count - 1}).");

        return Ok(session.OpenXmlInfo.Slides[slideIndex]);
    }
}
