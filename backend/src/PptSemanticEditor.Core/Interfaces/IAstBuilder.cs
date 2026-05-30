using PptSemanticEditor.Core.Models;
using PptSemanticEditor.Core.Models.Ast;

namespace PptSemanticEditor.Core.Interfaces;

public interface IAstBuilder
{
    PresentationRootNode BuildAst(OpenXmlInfo openXmlInfo);
}
