using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Core.Interfaces;

public interface ILlmService
{
    Task<AnalysisResult> AnalyzeAsync(string semanticJson);
    Task<List<EditAction>> GenerateEditPlanAsync(string semanticJson, string userPrompt);
    Task<string> ApplyTextRewriteAsync(string originalText, string instruction);
}
