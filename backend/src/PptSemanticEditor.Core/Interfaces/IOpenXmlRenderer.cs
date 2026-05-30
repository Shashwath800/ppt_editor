using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Core.Interfaces;

public interface IOpenXmlRenderer
{
    /// <param name="presentation">The modified semantic presentation data</param>
    /// <param name="originalFilePath">Path to the original uploaded PPTX file</param>
    /// <returns>A stream containing the modified PPTX</returns>
    Task<Stream> RenderAsync(SemanticPresentation presentation, string originalFilePath);
}
