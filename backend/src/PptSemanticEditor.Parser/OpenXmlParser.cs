using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Parser;

public class OpenXmlParser : IOpenXmlParser
{
    private readonly ShapeExtractor _shapeExtractor = new();
    private readonly RelationshipExtractor _relationshipExtractor = new();
    private readonly MediaExtractor _mediaExtractor = new();

    public Task<OpenXmlInfo> ParseAsync(Stream stream, string fileName)
    {
        var result = new OpenXmlInfo { FileName = fileName };

        using var presentationDocument = PresentationDocument.Open(stream, false);
        var presentationPart = presentationDocument.PresentationPart;

        if (presentationPart == null)
            throw new InvalidOperationException("Invalid PPTX: No presentation part found.");

        // Extract slide dimensions
        var slideSize = presentationPart.Presentation.SlideSize;
        if (slideSize != null)
        {
            // EMU to inches (1 inch = 914400 EMU)
            result.SlideWidth = (slideSize.Cx?.Value ?? 9144000) / 914400.0;
            result.SlideHeight = (slideSize.Cy?.Value ?? 6858000) / 914400.0;
        }
        else
        {
            result.SlideWidth = 10.0;
            result.SlideHeight = 7.5;
        }

        // Process each slide
        var slideIdList = presentationPart.Presentation.SlideIdList;
        if (slideIdList == null)
            return Task.FromResult(result);

        int slideIndex = 0;
        foreach (var slideId in slideIdList.Elements<SlideId>())
        {
            var relationshipId = slideId.RelationshipId?.Value;
            if (string.IsNullOrEmpty(relationshipId))
            {
                slideIndex++;
                continue;
            }

            var slidePart = (SlidePart)presentationPart.GetPartById(relationshipId);
            var slideInfo = ExtractSlideInfo(slidePart, slideIndex);
            result.Slides.Add(slideInfo);
            slideIndex++;
        }

        return Task.FromResult(result);
    }

    private OpenXmlSlideInfo ExtractSlideInfo(SlidePart slidePart, int slideIndex)
    {
        var slideInfo = new OpenXmlSlideInfo
        {
            SlideIndex = slideIndex
        };

        // Extract raw XML
        using (var xmlStream = slidePart.GetStream())
        using (var reader = new StreamReader(xmlStream))
        {
            slideInfo.RawXml = reader.ReadToEnd();
        }

        // Extract shapes
        var slide = slidePart.Slide;
        if (slide?.CommonSlideData?.ShapeTree != null)
        {
            slideInfo.Shapes = _shapeExtractor.ExtractShapes(slide.CommonSlideData.ShapeTree, slidePart);
        }

        // Extract relationships
        slideInfo.Relationships = _relationshipExtractor.ExtractRelationships(slidePart);

        // Extract media (images)
        _mediaExtractor.ExtractMedia(slidePart, slideInfo.Shapes);

        return slideInfo;
    }
}
