using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Core.Interfaces;

public interface IValidationEngine
{
    ValidationResult Validate(SemanticPresentation presentation);
}
