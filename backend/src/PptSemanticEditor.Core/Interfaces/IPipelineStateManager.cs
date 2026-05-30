using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Core.Interfaces;

public interface IPipelineStateManager
{
    PipelineState CreateSession(string fileName);
    PipelineState? GetSession(string sessionId);
    void UpdateStage(string sessionId, string stage, string status);
    void CompleteStage(string sessionId, string stage);
    void FailStage(string sessionId, string stage);
}
