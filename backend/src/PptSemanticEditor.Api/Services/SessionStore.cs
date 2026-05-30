using System.Collections.Concurrent;
using PptSemanticEditor.Core.Models;
using PptSemanticEditor.Core.Models.Ast;

namespace PptSemanticEditor.Api.Services;

/// <summary>
/// In-memory session storage for all intermediate pipeline data.
/// Each upload creates a session that holds parsed data through all stages.
/// </summary>
public class SessionStore
{
    private readonly ConcurrentDictionary<string, SessionData> _sessions = new();

    public SessionData CreateSession(string fileName)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..12];
        var session = new SessionData
        {
            SessionId = sessionId,
            FileName = fileName,
            PipelineState = new PipelineState
            {
                SessionId = sessionId,
                FileName = fileName
            }
        };
        _sessions[sessionId] = session;
        return session;
    }

    public SessionData? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public void UpdateSession(string sessionId, Action<SessionData> update)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            update(session);
        }
    }
}

public class SessionData
{
    public string SessionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public PipelineState PipelineState { get; set; } = new();

    // Pipeline intermediate data
    public OpenXmlInfo? OpenXmlInfo { get; set; }
    public PresentationRootNode? Ast { get; set; }
    public SemanticPresentation? SemanticPresentation { get; set; }
    public SemanticPresentation? ModifiedPresentation { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
    public List<ActionCommand>? ActionCommands { get; set; }
    public VersionHistory VersionHistory { get; set; } = new();
    public ValidationResult? LastValidation { get; set; }
    public string? RenderedFilePath { get; set; }
}
