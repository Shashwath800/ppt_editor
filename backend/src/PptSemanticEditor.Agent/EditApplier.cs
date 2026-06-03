using System.Text.Json;
using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Agent;

public class EditApplier : IEditApplier
{
    private readonly ILlmService _llmService;
    private readonly IActionExecutor _actionExecutor;

    public EditApplier(ILlmService llmService, IActionExecutor actionExecutor)
    {
        _llmService = llmService;
        _actionExecutor = actionExecutor;
    }

    public async Task<SemanticPresentation> ApplyEditsAsync(
        SemanticPresentation presentation,
        List<EditAction> actions)
    {
        // Convert old EditAction to new ActionCommand
        var commands = actions.Select(a => new ActionCommand
        {
            Action = a.Action,
            Slide = a.Slide,
            Target = a.Target,
            Description = a.Description,
            Reason = a.Reason,
            Confidence = a.Confidence,
            Parameters = a.Parameters,
            Approved = a.Approved
        }).ToList();

        var result = await _actionExecutor.ExecuteAsync(presentation, commands);
        return result.Modified;
    }

    private async Task ApplyAction(SemanticPresentation presentation, EditAction action)
    {
        switch (action.Action.ToLowerInvariant())
        {
            case "rewrite_text":
                await ApplyRewriteText(presentation, action);
                break;
            case "increase_font":
                ApplyFontSize(presentation, action);
                break;
            case "decrease_font":
                ApplyFontSize(presentation, action);
                break;
            case "change_color":
                ApplyChangeColor(presentation, action);
                break;
            case "remove_element":
                ApplyRemoveElement(presentation, action);
                break;
            case "add_element":
                ApplyAddElement(presentation, action);
                break;
            case "reorder_slides":
                ApplyReorderSlides(presentation, action);
                break;
        }
    }

    private async Task ApplyRewriteText(SemanticPresentation presentation, EditAction action)
    {
        var element = FindElement(presentation, action.Slide, action.Target);
        if (element == null) return;

        if (action.Parameters.TryGetValue("newText", out var newTextObj))
        {
            var newText = newTextObj?.ToString();
            if (!string.IsNullOrEmpty(newText))
            {
                element.Text = newText.Replace("\\n", "\n");
                return;
            }
        }

        // If no newText provided, use LLM to rewrite
        var rewritten = await _llmService.ApplyTextRewriteAsync(
            element.Text,
            action.Description
        );
        element.Text = rewritten.Replace("\\n", "\n");
    }

    private void ApplyFontSize(SemanticPresentation presentation, EditAction action)
    {
        var element = FindElement(presentation, action.Slide, action.Target);
        if (element == null) return;

        if (action.Parameters.TryGetValue("newSize", out var sizeObj))
        {
            if (sizeObj is JsonElement jsonEl && jsonEl.TryGetDouble(out var size))
            {
                element.FontSize = size;
            }
            else if (double.TryParse(sizeObj?.ToString(), out var parsedSize))
            {
                element.FontSize = parsedSize;
            }
        }
    }

    private void ApplyChangeColor(SemanticPresentation presentation, EditAction action)
    {
        var element = FindElement(presentation, action.Slide, action.Target);
        if (element == null) return;

        var property = action.Parameters.GetValueOrDefault("property")?.ToString() ?? "fontColor";
        var newColor = action.Parameters.GetValueOrDefault("newColor")?.ToString() ?? "#000000";

        if (property == "fillColor")
            element.FillColor = newColor;
        else
            element.FontColor = newColor;
    }

    private void ApplyRemoveElement(SemanticPresentation presentation, EditAction action)
    {
        if (action.Slide == null || string.IsNullOrEmpty(action.Target)) return;

        var slideIndex = action.Slide.Value - 1;
        if (slideIndex < 0 || slideIndex >= presentation.Slides.Count) return;

        var slide = presentation.Slides[slideIndex];
        slide.Elements.RemoveAll(e => e.Id == action.Target);
    }

    private void ApplyAddElement(SemanticPresentation presentation, EditAction action)
    {
        if (action.Slide == null) return;

        var slideIndex = action.Slide.Value - 1;
        if (slideIndex < 0 || slideIndex >= presentation.Slides.Count) return;

        var slide = presentation.Slides[slideIndex];
        var newElement = new SemanticElement
        {
            Id = $"element_new_{Guid.NewGuid():N}",
            Type = GetStringParam(action, "type", "text"),
            Label = GetStringParam(action, "label", "New Element"),
            Text = GetStringParam(action, "text", ""),
            X = GetDoubleParam(action, "x", 1.0),
            Y = GetDoubleParam(action, "y", 1.0),
            Width = GetDoubleParam(action, "width", 4.0),
            Height = GetDoubleParam(action, "height", 1.0),
            FontSize = GetDoubleParam(action, "fontSize", 18),
            FontColor = GetStringParam(action, "fontColor", "#000000"),
            FillColor = GetStringParam(action, "fillColor", "transparent")
        };

        slide.Elements.Add(newElement);
    }

    private void ApplyReorderSlides(SemanticPresentation presentation, EditAction action)
    {
        if (!action.Parameters.TryGetValue("newOrder", out var orderObj)) return;

        List<int>? newOrder = null;
        if (orderObj is JsonElement jsonEl && jsonEl.ValueKind == JsonValueKind.Array)
        {
            newOrder = jsonEl.EnumerateArray()
                .Where(e => e.TryGetInt32(out _))
                .Select(e => e.GetInt32())
                .ToList();
        }

        if (newOrder == null || newOrder.Count != presentation.Slides.Count) return;

        var reordered = new List<SemanticSlide>();
        foreach (var idx in newOrder)
        {
            var slideIdx = idx - 1; // Convert 1-based to 0-based
            if (slideIdx >= 0 && slideIdx < presentation.Slides.Count)
                reordered.Add(presentation.Slides[slideIdx]);
        }

        if (reordered.Count == presentation.Slides.Count)
        {
            presentation.Slides = reordered;
            // Re-number slides
            for (int i = 0; i < presentation.Slides.Count; i++)
                presentation.Slides[i].Id = i + 1;
        }
    }

    private SemanticElement? FindElement(SemanticPresentation presentation, int? slideNumber, string? targetId)
    {
        if (slideNumber != null)
        {
            var slideIndex = slideNumber.Value - 1;
            if (slideIndex >= 0 && slideIndex < presentation.Slides.Count)
            {
                return presentation.Slides[slideIndex].Elements
                    .FirstOrDefault(e => e.Id == targetId);
            }
        }

        // Search all slides if no slide specified
        foreach (var slide in presentation.Slides)
        {
            var element = slide.Elements.FirstOrDefault(e => e.Id == targetId);
            if (element != null) return element;
        }

        return null;
    }

    private string GetStringParam(EditAction action, string key, string defaultValue)
    {
        if (action.Parameters.TryGetValue(key, out var val))
        {
            if (val is JsonElement jsonEl)
                return jsonEl.GetString() ?? defaultValue;
            return val?.ToString() ?? defaultValue;
        }
        return defaultValue;
    }

    private double GetDoubleParam(EditAction action, string key, double defaultValue)
    {
        if (action.Parameters.TryGetValue(key, out var val))
        {
            if (val is JsonElement jsonEl && jsonEl.TryGetDouble(out var d))
                return d;
            if (double.TryParse(val?.ToString(), out var parsed))
                return parsed;
        }
        return defaultValue;
    }
}
