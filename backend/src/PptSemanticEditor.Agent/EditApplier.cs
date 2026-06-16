using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Agent;

public class EditApplier : IEditApplier
{
    private readonly IActionExecutor _actionExecutor;

    public EditApplier(ILlmService llmService, IActionExecutor actionExecutor)
    {
        // llmService parameter kept for DI compatibility; not used here.
        // All action execution is handled by ActionExecutor.
        _actionExecutor = actionExecutor;
    }

    /// <summary>
    /// Applies edit actions by delegating entirely to <see cref="IActionExecutor"/>.
    /// ActionExecutor handles all action types (rewrite_text, add_slide, font changes, etc.).
    /// </summary>
    public async Task<SemanticPresentation> ApplyEditsAsync(
        SemanticPresentation presentation,
        List<EditAction> actions)
    {
        // Convert old EditAction to new ActionCommand
        var commands = actions.Select(a => new ActionCommand
        {
            Action = a.Action,
            Slide = a.Slide,
            Target = a.Target,
            Description = a.Description,
            Reason = a.Reason,
            Confidence = a.Confidence,
            Parameters = a.Parameters,
            Approved = a.Approved
        }).ToList();

        var result = await _actionExecutor.ExecuteAsync(presentation, commands);
        return result.Modified;
    }
}
