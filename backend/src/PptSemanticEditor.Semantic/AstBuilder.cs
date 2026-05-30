using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;
using PptSemanticEditor.Core.Models.Ast;

namespace PptSemanticEditor.Semantic;

/// <summary>
/// Builds the Presentation AST from raw OpenXML data.
/// The AST becomes the canonical intermediate representation — 
/// SemanticTreeBuilder then builds from this AST.
/// </summary>
public class AstBuilder : IAstBuilder
{
    private const double EmuPerInch = 914400.0;

    public PresentationRootNode BuildAst(OpenXmlInfo openXmlInfo)
    {
        var root = new PresentationRootNode
        {
            FileName = openXmlInfo.FileName,
            SlideWidth = openXmlInfo.SlideWidth,
            SlideHeight = openXmlInfo.SlideHeight,
            SlideCount = openXmlInfo.Slides.Count
        };
        root.Metadata["source"] = "openxml";
        root.Metadata["parsedAt"] = DateTime.UtcNow.ToString("o");

        foreach (var slideInfo in openXmlInfo.Slides)
        {
            var slideNode = BuildSlideNode(slideInfo);
            root.Children.Add(slideNode);
        }

        return root;
    }

    private SlideNode BuildSlideNode(OpenXmlSlideInfo slideInfo)
    {
        var slideNode = new SlideNode
        {
            NodeId = $"slide_{slideInfo.SlideIndex}",
            SlideIndex = slideInfo.SlideIndex
        };
        slideNode.Metadata["rawXmlLength"] = slideInfo.RawXml.Length.ToString();
        slideNode.Metadata["shapeCount"] = slideInfo.Shapes.Count.ToString();

        int zIndex = 0;
        bool foundTitle = false;

        foreach (var shape in slideInfo.Shapes)
        {
            PresentationNode? childNode = null;

            if (shape.IsConnector)
            {
                childNode = new ConnectorNode
                {
                    NodeId = $"connector_{shape.ShapeId}",
                    FromNodeId = shape.ConnectorStartId,
                    ToNodeId = shape.ConnectorEndId,
                    ConnectorType = "straight"
                };
            }
            else if (shape.Type == "image")
            {
                var imgNode = new ImageNode
                {
                    NodeId = $"image_{shape.ShapeId}",
                    ImageBase64 = shape.ImageBase64,
                    ContentType = "image/png"
                };
                imgNode.Metadata["shapeId"] = shape.ShapeId;
                childNode = imgNode;
            }
            else
            {
                // Build ShapeNode wrapping a TextNode
                var shapeNode = new ShapeNode
                {
                    NodeId = $"shape_{shape.ShapeId}",
                    ShapeId = shape.ShapeId,
                    ShapeKind = shape.Type,
                    X = Math.Round(shape.OffsetX / EmuPerInch, 3),
                    Y = Math.Round(shape.OffsetY / EmuPerInch, 3),
                    Width = Math.Round(shape.ExtentCx / EmuPerInch, 3),
                    Height = Math.Round(shape.ExtentCy / EmuPerInch, 3),
                    FillColor = shape.FillColor,
                    ZIndex = zIndex++
                };
                shapeNode.Metadata["name"] = shape.Name;
                shapeNode.Metadata["originalType"] = shape.Type;

                if (!string.IsNullOrWhiteSpace(shape.Text))
                {
                    var isTitle = shape.IsTitle && !foundTitle;
                    if (isTitle) foundTitle = true;

                    var textNode = new TextNode
                    {
                        NodeId = $"text_{shape.ShapeId}",
                        Content = shape.Text,
                        FontSize = shape.FontSize > 0 ? shape.FontSize : 18,
                        FontColor = !string.IsNullOrEmpty(shape.FontColor) ? shape.FontColor : "#000000",
                        IsTitle = isTitle
                    };
                    shapeNode.Children.Add(textNode);

                    if (isTitle) slideNode.SlideTitle = shape.Text.Split('\n').First().Trim();
                }

                childNode = shapeNode;
            }

            if (childNode != null)
                slideNode.Children.Add(childNode);
        }

        // Infer title if not found from placeholder
        if (string.IsNullOrEmpty(slideNode.SlideTitle))
        {
            var bestText = slideNode.Children
                .OfType<ShapeNode>()
                .SelectMany(s => s.Children.OfType<TextNode>())
                .Where(t => !string.IsNullOrWhiteSpace(t.Content) && t.Content.Length < 200)
                .OrderByDescending(t => t.FontSize)
                .FirstOrDefault();
            slideNode.SlideTitle = bestText?.Content?.Split('\n').First().Trim() ?? "Untitled Slide";
        }

        return slideNode;
    }
}
