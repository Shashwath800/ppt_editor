using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Core.Interfaces;

public interface IEditApplier
{
    Task<SemanticPresentation> ApplyEditsAsync(SemanticPresentation presentation, List<EditAction> actions);
}
