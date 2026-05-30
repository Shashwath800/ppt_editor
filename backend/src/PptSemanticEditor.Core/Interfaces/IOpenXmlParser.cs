using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Core.Interfaces;

public interface IOpenXmlParser
{
    Task<OpenXmlInfo> ParseAsync(Stream stream, string fileName);
}
