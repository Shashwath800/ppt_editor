using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Core.Interfaces;

public interface IActionExecutor
{
    Task<(SemanticPresentation Modified, List<ActionCommand> AuditLog)> ExecuteAsync(
        SemanticPresentation presentation,
        List<ActionCommand> commands);
}
