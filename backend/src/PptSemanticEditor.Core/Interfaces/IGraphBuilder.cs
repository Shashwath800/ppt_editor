using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Core.Interfaces;

public interface IGraphBuilder
{
    SemanticGraph? BuildGraph(OpenXmlSlideInfo slide);
}
