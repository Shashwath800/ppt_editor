using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Agent;

public class AnalysisAgent
{
    private readonly ILlmService _llmService;

    public AnalysisAgent(ILlmService llmService)
    {
        _llmService = llmService;
    }

    public async Task<AnalysisResult> AnalyzeAsync(string semanticJson)
    {
        return await _llmService.AnalyzeAsync(semanticJson);
    }
}
