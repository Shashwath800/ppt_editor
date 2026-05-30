namespace PptSemanticEditor.Core.Models;

public enum SlideClassification
{
    TitleSlide,
    BulletSlide,
    Timeline,
    Flowchart,
    ArchitectureDiagram,
    ComparisonSlide,
    TableSlide,
    ChartSlide,
    ImageSlide,
    ContentSlide,
    Unknown
}

public static class SlideClassificationExtensions
{
    public static string ToFriendlyString(this SlideClassification classification)
    {
        return classification switch
        {
            SlideClassification.TitleSlide => "title_slide",
            SlideClassification.BulletSlide => "bullet_slide",
            SlideClassification.Timeline => "timeline",
            SlideClassification.Flowchart => "flowchart",
            SlideClassification.ArchitectureDiagram => "architecture_diagram",
            SlideClassification.ComparisonSlide => "comparison_slide",
            SlideClassification.TableSlide => "table_slide",
            SlideClassification.ChartSlide => "chart_slide",
            SlideClassification.ImageSlide => "image_slide",
            SlideClassification.ContentSlide => "content_slide",
            _ => "unknown"
        };
    }

    public static string ToDisplayName(this SlideClassification classification)
    {
        return classification switch
        {
            SlideClassification.TitleSlide => "Title Slide",
            SlideClassification.BulletSlide => "Bullet Slide",
            SlideClassification.Timeline => "Timeline",
            SlideClassification.Flowchart => "Flowchart",
            SlideClassification.ArchitectureDiagram => "Architecture Diagram",
            SlideClassification.ComparisonSlide => "Comparison Slide",
            SlideClassification.TableSlide => "Table Slide",
            SlideClassification.ChartSlide => "Chart Slide",
            SlideClassification.ImageSlide => "Image Slide",
            SlideClassification.ContentSlide => "Content Slide",
            _ => "Unknown"
        };
    }
}
