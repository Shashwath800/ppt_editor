using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using PptSemanticEditor.Core.Models;

using Picture = DocumentFormat.OpenXml.Presentation.Picture;

namespace PptSemanticEditor.Parser;

public class MediaExtractor
{
    public void ExtractMedia(SlidePart slidePart, List<OpenXmlShapeInfo> shapes)
    {
        foreach (var shape in shapes.Where(s => s.Type == "image"))
        {
            try
            {
                // Find the picture element in the slide
                var pictures = slidePart.Slide.Descendants<Picture>();
                foreach (var picture in pictures)
                {
                    var nvPicPr = picture.NonVisualPictureProperties;
                    var shapeId = nvPicPr?.NonVisualDrawingProperties?.Id?.Value.ToString();

                    if (shapeId == shape.ShapeId)
                    {
                        // Get the blip (image reference)
                        var blipFill = picture.BlipFill;
                        var blip = blipFill?.Blip;
                        var embedId = blip?.Embed?.Value;

                        if (!string.IsNullOrEmpty(embedId))
                        {
                            var imagePart = (ImagePart)slidePart.GetPartById(embedId);
                            using var imageStream = imagePart.GetStream();
                            using var memStream = new MemoryStream();
                            imageStream.CopyTo(memStream);
                            var base64 = Convert.ToBase64String(memStream.ToArray());

                            // Determine content type for data URI
                            var contentType = imagePart.ContentType;
                            shape.ImageBase64 = $"data:{contentType};base64,{base64}";
                        }
                        break;
                    }
                }
            }
            catch
            {
                // If media extraction fails, continue without the image
                shape.ImageBase64 = null;
            }
        }
    }
}
