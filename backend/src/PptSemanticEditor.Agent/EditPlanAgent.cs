using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Agent;

public class EditPlanAgent
{
    private readonly ILlmService _llmService;

    public EditPlanAgent(ILlmService llmService)
    {
        _llmService = llmService;
    }

    public async Task<List<EditAction>> GenerateEditPlanAsync(string semanticJson, string userPrompt)
    {
        return await _llmService.GenerateEditPlanAsync(semanticJson, userPrompt);
    }
}
