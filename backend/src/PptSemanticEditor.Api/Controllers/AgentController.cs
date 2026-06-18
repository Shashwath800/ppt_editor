using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PptSemanticEditor.Agent;
using PptSemanticEditor.Api.Services;
using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;
using PptSemanticEditor.Semantic;

namespace PptSemanticEditor.Api.Controllers;

[ApiController]
[Route("api/agent")]
public class AgentController : ControllerBase
{
    private readonly SessionStore _sessionStore;
    private readonly AnalysisAgent _analysisAgent;
    private readonly EditPlanAgent _editPlanAgent;
    private readonly IActionExecutor _actionExecutor;
    private readonly IValidationEngine _validationEngine;
    private readonly JsonGenerator _jsonGenerator;

    public AgentController(
        SessionStore sessionStore,
        AnalysisAgent analysisAgent,
        EditPlanAgent editPlanAgent,
        IActionExecutor actionExecutor,
        IValidationEngine validationEngine,
        JsonGenerator jsonGenerator)
    {
        _sessionStore = sessionStore;
        _analysisAgent = analysisAgent;
        _editPlanAgent = editPlanAgent;
        _actionExecutor = actionExecutor;
        _validationEngine = validationEngine;
        _jsonGenerator = jsonGenerator;
    }

    public class EditPlanRequest
    {
        public string Prompt { get; set; } = string.Empty;
    }

    [HttpPost("{sessionId}/analyze")]
    public async Task<IActionResult> Analyze(string sessionId)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        if (session.SemanticPresentation == null)
            return BadRequest("Semantic JSON not yet available. Upload a file first.");

        try
        {
            var minifiedPresentation = new
            {
                session.SemanticPresentation.SlideCount,
                Slides = session.SemanticPresentation.Slides.Select(s => new
                {
                    s.Id,
                    s.Title,
                    Elements = s.Elements
                        .Where(e => !string.IsNullOrWhiteSpace(e.Text))
                        .Select(e => new
                        {
                            e.Id,
                            e.Type,
                            e.Text
                        })
                })
            };
            var semanticJson = JsonSerializer.Serialize(minifiedPresentation, JsonGenerator.GetOptions());
            var result = await _analysisAgent.AnalyzeAsync(semanticJson);

            session.AnalysisResult = result;
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Analysis failed: {ex.Message}");
        }
    }

    [HttpPost("{sessionId}/edit-plan")]
    public async Task<IActionResult> GenerateEditPlan(string sessionId, [FromBody] EditPlanRequest request)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        if (session.SemanticPresentation == null)
            return BadRequest("Semantic JSON not yet available.");

        var userPrompt = request?.Prompt ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userPrompt))
            return BadRequest("An edit instruction (prompt) is required.");

        session.PipelineState.SetStage("editPlan", "inProgress");

        try
        {
            // Use current state (modified if available) so the AI sees post-rollback data
            var currentPresentation = session.ModifiedPresentation ?? session.SemanticPresentation;
            
            // If the user prompt references a specific slide number, filter to only that slide
            var slidesToSend = currentPresentation!.Slides;
            var slideMatch = System.Text.RegularExpressions.Regex.Match(
                userPrompt, @"slide\s+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (slideMatch.Success && int.TryParse(slideMatch.Groups[1].Value, out var slideNum))
            {
                var filtered = slidesToSend.Where(s => s.Id == slideNum).ToList();
                if (filtered.Count > 0)
                    slidesToSend = filtered;
            }

            // Send only text content to the LLM — no structural metadata
            // Group elements visually under each slide
            var slideGroups = slidesToSend.Select(slide =>
            {
                var lines = slide.Elements
                    .Where(e => !string.IsNullOrWhiteSpace(e.Text))
                    .Select(e =>
                    {
                        var escapedText = e.Text.Replace("\n", "\\n");
                        var wordCount = e.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                        return $"  [{e.Id}] (max {wordCount} words): \"{escapedText}\"";
                    });
                return $"--- Slide {slide.Id} ---\n" + string.Join("\n", lines);
            });
            var textContent = string.Join("\n\n", slideGroups);
            var editActions = await _editPlanAgent.GenerateEditPlanAsync(textContent, userPrompt);

            // Convert EditActions to ActionCommands for backward compatibility with the AI prompt if it still generates EditActions
            var commands = editActions.Select(a => new ActionCommand
            {
                Action = a.Action,
                Slide = a.Slide,
                Target = a.Target,
                Description = a.Description,
                Reason = a.Reason,
                Confidence = a.Confidence,
                Parameters = a.Parameters,
                Approved = true // Always approve LLM-generated actions; user reviews them in the UI before applying
            }).ToList();

            session.ActionCommands = commands;
            session.PipelineState.CompleteStage("editPlan");

            return Ok(commands);
        }
        catch (Exception ex)
        {
            session.PipelineState.FailStage("editPlan");
            return StatusCode(500, $"Edit plan generation failed: {ex.Message}");
        }
    }

    [HttpPost("{sessionId}/apply-edits")]
    public async Task<IActionResult> ApplyEdits(string sessionId, [FromBody] List<ActionCommand> commands)
    {
        var session = _sessionStore.GetSession(sessionId);
        if (session == null)
            return NotFound("Session not found.");

        if (session.SemanticPresentation == null)
            return BadRequest("Semantic JSON not yet available.");

        session.PipelineState.SetStage("jsonTransformation", "inProgress");
        session.PipelineState.SetStage("validation", "inProgress");

        try
        {
            // Use current state as the base — after rollback this is the rolled-back version
            var currentPresentation = session.ModifiedPresentation ?? session.SemanticPresentation;
            var (modified, auditLog) = await _actionExecutor.ExecuteAsync(currentPresentation!, commands);

            session.ModifiedPresentation = modified;
            session.ActionCommands = auditLog;
            session.PipelineState.CompleteStage("jsonTransformation");

            // Run Validation
            var validationResult = _validationEngine.Validate(modified);
            session.LastValidation = validationResult;
            session.PipelineState.CompleteStage("validation");

            // Save Version Snapshot
            session.VersionHistory.AddVersion(modified, "Applied Edits", auditLog);

            return Ok(new { Presentation = modified, Validation = validationResult, AuditLog = auditLog });
        }
        catch (Exception ex)
        {
            session.PipelineState.FailStage("jsonTransformation");
            session.PipelineState.FailStage("validation");
            return StatusCode(500, $"Edit application failed: {ex.Message}");
        }
    }
}
