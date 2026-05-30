using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Semantic;

/// <summary>
/// Runs pre-render validation checks on the semantic presentation.
/// Errors are blocking; warnings are informational.
/// </summary>
public class ValidationEngine : IValidationEngine
{
    public ValidationResult Validate(SemanticPresentation presentation)
    {
        var result = new ValidationResult();

        ValidateStructural(presentation, result);
        ValidateSlides(presentation, result);
        ValidateGraphs(presentation, result);

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    private void ValidateStructural(SemanticPresentation p, ValidationResult r)
    {
        // Slide count mismatch
        if (p.SlideCount != p.Slides.Count)
        {
            r.Errors.Add(new ValidationError
            {
                Code = "SLIDE_COUNT_MISMATCH",
                Message = $"slideCount ({p.SlideCount}) does not match actual slides ({p.Slides.Count})",
                Target = "presentation",
                Severity = "error"
            });
        }

        // Duplicate element IDs across the whole presentation
        var allIds = p.Slides.SelectMany(s => s.Elements).Select(e => e.Id).ToList();
        var duplicates = allIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var dup in duplicates)
        {
            r.Errors.Add(new ValidationError
            {
                Code = "DUPLICATE_ID",
                Message = $"Element ID '{dup}' appears multiple times across slides",
                Target = dup,
                Severity = "error"
            });
        }

        // Invalid dimensions
        if (p.SlideWidth <= 0 || p.SlideHeight <= 0)
        {
            r.Errors.Add(new ValidationError
            {
                Code = "INVALID_DIMENSIONS",
                Message = $"Invalid slide dimensions: {p.SlideWidth}x{p.SlideHeight}",
                Target = "presentation",
                Severity = "error"
            });
        }
    }

    private void ValidateSlides(SemanticPresentation p, ValidationResult r)
    {
        for (int i = 0; i < p.Slides.Count; i++)
        {
            var slide = p.Slides[i];
            var slideNum = i + 1;

            // Missing title
            if (string.IsNullOrWhiteSpace(slide.Title))
            {
                r.Warnings.Add(new ValidationWarning
                {
                    Code = "MISSING_TITLE",
                    Message = $"Slide {slideNum} has no title",
                    Target = $"slide_{slide.Id}",
                    Slide = slideNum
                });
            }

            // No elements
            if (slide.Elements.Count == 0)
            {
                r.Warnings.Add(new ValidationWarning
                {
                    Code = "EMPTY_SLIDE",
                    Message = $"Slide {slideNum} has no elements",
                    Target = $"slide_{slide.Id}",
                    Slide = slideNum
                });
            }

            foreach (var element in slide.Elements)
            {
                // Invalid coordinates
                if (element.X < 0 || element.Y < 0)
                {
                    r.Warnings.Add(new ValidationWarning
                    {
                        Code = "NEGATIVE_COORDINATES",
                        Message = $"Element '{element.Id}' has negative position ({element.X}, {element.Y})",
                        Target = element.Id,
                        Slide = slideNum
                    });
                }

                // Out-of-bounds (beyond slide dimensions)
                if (element.X + element.Width > p.SlideWidth + 0.5 ||
                    element.Y + element.Height > p.SlideHeight + 0.5)
                {
                    r.Warnings.Add(new ValidationWarning
                    {
                        Code = "OUT_OF_BOUNDS",
                        Message = $"Element '{element.Id}' extends beyond slide boundaries",
                        Target = element.Id,
                        Slide = slideNum
                    });
                }
            }

            // Overlapping elements
            var elements = slide.Elements.Where(e => e.Width > 0 && e.Height > 0).ToList();
            for (int j = 0; j < elements.Count - 1; j++)
            {
                for (int k = j + 1; k < elements.Count; k++)
                {
                    if (Overlaps(elements[j], elements[k]))
                    {
                        r.Warnings.Add(new ValidationWarning
                        {
                            Code = "OVERLAPPING_ELEMENTS",
                            Message = $"Elements '{elements[j].Id}' and '{elements[k].Id}' overlap",
                            Target = elements[j].Id,
                            Slide = slideNum
                        });
                        break; // One warning per element pair is enough
                    }
                }
            }

            // Broken relationship references
            var elementIds = slide.Elements.Select(e => e.Id).ToHashSet();
            foreach (var rel in slide.Relationships)
            {
                if (!elementIds.Contains(rel.From))
                {
                    r.Errors.Add(new ValidationError
                    {
                        Code = "BROKEN_REFERENCE",
                        Message = $"Relationship references unknown element '{rel.From}'",
                        Target = rel.From,
                        Severity = "error",
                        Slide = slideNum
                    });
                }
                if (!elementIds.Contains(rel.To))
                {
                    r.Errors.Add(new ValidationError
                    {
                        Code = "BROKEN_REFERENCE",
                        Message = $"Relationship references unknown element '{rel.To}'",
                        Target = rel.To,
                        Severity = "error",
                        Slide = slideNum
                    });
                }
            }
        }
    }

    private void ValidateGraphs(SemanticPresentation p, ValidationResult r)
    {
        for (int i = 0; i < p.Slides.Count; i++)
        {
            var slide = p.Slides[i];
            var slideNum = i + 1;

            if (slide.Graph == null) continue;

            var nodeIds = slide.Graph.Nodes.Select(n => n.Id).ToHashSet();

            // Invalid edges (referencing non-existent nodes)
            foreach (var edge in slide.Graph.Edges)
            {
                if (!nodeIds.Contains(edge.From))
                {
                    r.Errors.Add(new ValidationError
                    {
                        Code = "INVALID_EDGE",
                        Message = $"Graph edge references unknown node '{edge.From}'",
                        Target = edge.From,
                        Severity = "error",
                        Slide = slideNum
                    });
                }
                if (!nodeIds.Contains(edge.To))
                {
                    r.Errors.Add(new ValidationError
                    {
                        Code = "INVALID_EDGE",
                        Message = $"Graph edge references unknown node '{edge.To}'",
                        Target = edge.To,
                        Severity = "error",
                        Slide = slideNum
                    });
                }
            }

            // Orphan nodes (in graph but no edges)
            var connectedNodes = slide.Graph.Edges
                .SelectMany(e => new[] { e.From, e.To })
                .ToHashSet();
            foreach (var node in slide.Graph.Nodes)
            {
                if (!connectedNodes.Contains(node.Id))
                {
                    r.Warnings.Add(new ValidationWarning
                    {
                        Code = "ORPHAN_NODE",
                        Message = $"Graph node '{node.Label}' has no edges",
                        Target = node.Id,
                        Slide = slideNum
                    });
                }
            }

            // Low-confidence geometry-inferred edges
            foreach (var edge in slide.Graph.Edges.Where(e => e.Confidence < 0.65))
            {
                r.Warnings.Add(new ValidationWarning
                {
                    Code = "LOW_CONFIDENCE_EDGE",
                    Message = $"Edge '{edge.From}' → '{edge.To}' has low confidence ({edge.Confidence:P0})",
                    Target = $"{edge.From}->{edge.To}",
                    Slide = slideNum
                });
            }

            // Simple circular reference detection (A→B→A)
            foreach (var edge in slide.Graph.Edges)
            {
                if (slide.Graph.Edges.Any(e => e.From == edge.To && e.To == edge.From))
                {
                    r.Warnings.Add(new ValidationWarning
                    {
                        Code = "CIRCULAR_REFERENCE",
                        Message = $"Circular edge detected between '{edge.From}' and '{edge.To}'",
                        Target = edge.From,
                        Slide = slideNum
                    });
                }
            }
        }
    }

    private static bool Overlaps(SemanticElement a, SemanticElement b)
    {
        return a.X < b.X + b.Width &&
               a.X + a.Width > b.X &&
               a.Y < b.Y + b.Height &&
               a.Y + a.Height > b.Y;
    }
}
