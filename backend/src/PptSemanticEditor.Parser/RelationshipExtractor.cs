using DocumentFormat.OpenXml.Packaging;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Parser;

public class RelationshipExtractor
{
    public List<OpenXmlRelationshipInfo> ExtractRelationships(SlidePart slidePart)
    {
        var relationships = new List<OpenXmlRelationshipInfo>();

        foreach (var rel in slidePart.Parts)
        {
            relationships.Add(new OpenXmlRelationshipInfo
            {
                Id = rel.RelationshipId,
                Type = GetRelationshipTypeName(rel.OpenXmlPart),
                Target = rel.OpenXmlPart.Uri.ToString()
            });
        }

        // Also include external relationships (hyperlinks, etc.)
        foreach (var extRel in slidePart.ExternalRelationships)
        {
            relationships.Add(new OpenXmlRelationshipInfo
            {
                Id = extRel.Id,
                Type = "external",
                Target = extRel.Uri?.ToString() ?? ""
            });
        }

        // Include hyperlink relationships
        foreach (var hyperlinkRel in slidePart.HyperlinkRelationships)
        {
            relationships.Add(new OpenXmlRelationshipInfo
            {
                Id = hyperlinkRel.Id,
                Type = "hyperlink",
                Target = hyperlinkRel.Uri?.ToString() ?? ""
            });
        }

        return relationships;
    }

    private string GetRelationshipTypeName(OpenXmlPart part)
    {
        return part switch
        {
            SlideLayoutPart => "slideLayout",
            ImagePart => "image",
            ChartPart => "chart",
            EmbeddedPackagePart => "embeddedPackage",
            NotesSlidePart => "notesSlide",
            _ => part.GetType().Name.Replace("Part", "").ToLowerInvariant()
        };
    }
}
