using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Core.Interfaces;

public interface ISemanticTreeBuilder
{
    SemanticPresentation BuildTree(OpenXmlInfo openXmlInfo);
}
