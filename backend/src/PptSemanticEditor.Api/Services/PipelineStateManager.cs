using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Api.Services;

public class PipelineStateManager : IPipelineStateManager
{
    private readonly SessionStore _sessionStore;

    public PipelineStateManager(SessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    public PipelineState CreateSession(string fileName)
    {
        var session = _sessionStore.CreateSession(fileName);
        return session.PipelineState;
    }

    public PipelineState? GetSession(string sessionId)
    {
        return _sessionStore.GetSession(sessionId)?.PipelineState;
    }

    public void UpdateStage(string sessionId, string stage, string status)
    {
        _sessionStore.UpdateSession(sessionId, session =>
        {
            session.PipelineState.SetStage(stage, status);
        });
    }

    public void CompleteStage(string sessionId, string stage)
    {
        UpdateStage(sessionId, stage, "completed");
    }

    public void FailStage(string sessionId, string stage)
    {
        UpdateStage(sessionId, stage, "error");
    }
}
