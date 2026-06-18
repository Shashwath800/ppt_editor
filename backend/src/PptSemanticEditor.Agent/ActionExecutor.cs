using System.Text.Json;
using System.Text.RegularExpressions;
using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Agent;

/// <summary>
/// Executes Action DSL commands against the semantic presentation.
/// Validates before applying, maintains consistency (e.g., removing node removes its edges),
/// and creates a full audit trail.
/// </summary>
public class ActionExecutor : IActionExecutor
{
    private readonly ILlmService _llmService;
    private static readonly Regex MultiSpaceRegex = new Regex(" {2,}", RegexOptions.Compiled);

    public ActionExecutor(ILlmService llmService)
    {
        _llmService = llmService;
    }

    public async Task<(SemanticPresentation Modified, List<ActionCommand> AuditLog)> ExecuteAsync(
        SemanticPresentation presentation,
        List<ActionCommand> commands)
    {
        // Deep clone
        var json = JsonSerializer.Serialize(presentation);
        var modified = JsonSerializer.Deserialize<SemanticPresentation>(json)!;
        var auditLog = new List<ActionCommand>();

        foreach (var command in commands.Where(c => c.Approved))
        {
            var entry = CloneCommand(command);
            try
            {
                await ApplyCommand(modified, command);
                entry.Result = "success";
                entry.AppliedAt = DateTime.UtcNow.ToString("o");
            }
            catch (Exception ex)
            {
                entry.Result = $"failed: {ex.Message}";
                Console.WriteLine($"[ActionExecutor] Failed '{command.Action}': {ex.Message}");
            }
            auditLog.Add(entry);
        }

        return (modified, auditLog);
    }

    private async Task ApplyCommand(SemanticPresentation presentation, ActionCommand cmd)
    {
        switch (cmd.Action.ToLowerInvariant())
        {
            case "rename_node":
                ApplyRenameNode(presentation, cmd);
                break;
            case "add_node":
                ApplyAddNode(presentation, cmd);
                break;
            case "remove_node":
                ApplyRemoveNode(presentation, cmd);
                break;
            case "move_node":
                ApplyMoveNode(presentation, cmd);
                break;
            case "add_edge":
                ApplyAddEdge(presentation, cmd);
                break;
            case "remove_edge":
                ApplyRemoveEdge(presentation, cmd);
                break;
            case "rewrite_text":
                await ApplyRewriteText(presentation, cmd);
                break;
            case "change_style":
                ApplyChangeStyle(presentation, cmd);
                break;
            case "add_slide":
                ApplyAddSlide(presentation, cmd);
                break;
            case "remove_slide":
                ApplyRemoveSlide(presentation, cmd);
                break;
            // Backward-compat with old EditAction types
            case "increase_font":
            case "decrease_font":
                ApplyFontSize(presentation, cmd);
                break;
            case "change_color":
                ApplyChangeColor(presentation, cmd);
                break;
            case "remove_element":
                ApplyRemoveNode(presentation, cmd);
                break;
            case "add_element":
                ApplyAddNode(presentation, cmd);
                break;
            case "reorder_slides":
                ApplyReorderSlides(presentation, cmd);
                break;
        }
    }

    private void ApplyRenameNode(SemanticPresentation p, ActionCommand cmd)
    {
        var element = FindElement(p, cmd.Slide, cmd.Target);
        if (element == null) throw new InvalidOperationException($"Element '{cmd.Target}' not found");

        var newLabel = cmd.Value ?? GetStringParam(cmd, "value", "");
        if (!string.IsNullOrEmpty(newLabel))
        {
            element.Label = newLabel;
            element.Text = newLabel;
        }

        // Also rename in graph nodes
        foreach (var slide in p.Slides)
        {
            var graphNode = slide.Graph?.Nodes.FirstOrDefault(n => n.Id == cmd.Target ||
                n.Id == element.Id.Replace("element_", ""));
            if (graphNode != null) graphNode.Label = newLabel ?? graphNode.Label;
        }
    }

    private void ApplyAddNode(SemanticPresentation p, ActionCommand cmd)
    {
        var slideIndex = (cmd.Slide ?? 1) - 1;
        if (slideIndex < 0 || slideIndex >= p.Slides.Count) return;

        var slide = p.Slides[slideIndex];
        var newEl = new SemanticElement
        {
            Id = $"element_new_{Guid.NewGuid():N}",
            Type = GetStringParam(cmd, "type", "shape"),
            Label = GetStringParam(cmd, "label", cmd.Value ?? "New Node"),
            Text = GetStringParam(cmd, "text", cmd.Value ?? ""),
            X = GetDoubleParam(cmd, "x", 1.0),
            Y = GetDoubleParam(cmd, "y", 1.0),
            Width = GetDoubleParam(cmd, "width", 2.0),
            Height = GetDoubleParam(cmd, "height", 1.0),
            FontSize = GetDoubleParam(cmd, "fontSize", 18),
            FontColor = GetStringParam(cmd, "fontColor", "#000000"),
            FillColor = GetStringParam(cmd, "fillColor", "#4472C4")
        };
        slide.Elements.Add(newEl);

        // Add to graph if slide has one
        if (slide.Graph != null)
        {
            slide.Graph.Nodes.Add(new SemanticGraphNode
            {
                Id = newEl.Id,
                Label = newEl.Label,
                X = newEl.X, Y = newEl.Y,
                Width = newEl.Width, Height = newEl.Height,
                NodeType = "process"
            });
        }
    }

    private void ApplyRemoveNode(SemanticPresentation p, ActionCommand cmd)
    {
        if (string.IsNullOrEmpty(cmd.Target)) return;
        var slideIndex = (cmd.Slide ?? 1) - 1;

        foreach (var slide in p.Slides)
        {
            var removed = slide.Elements.RemoveAll(e => e.Id == cmd.Target);
            if (removed > 0)
            {
                // Remove all relationships involving this node
                slide.Relationships.RemoveAll(r => r.From == cmd.Target || r.To == cmd.Target);
                // Remove from graph
                slide.Graph?.Nodes.RemoveAll(n => n.Id == cmd.Target);
                slide.Graph?.Edges.RemoveAll(e => e.From == cmd.Target || e.To == cmd.Target);
                break;
            }
        }
    }

    private void ApplyMoveNode(SemanticPresentation p, ActionCommand cmd)
    {
        var element = FindElement(p, cmd.Slide, cmd.Target);
        if (element == null) return;

        element.X = GetDoubleParam(cmd, "x", element.X);
        element.Y = GetDoubleParam(cmd, "y", element.Y);

        // Update graph node position too
        foreach (var slide in p.Slides)
        {
            var graphNode = slide.Graph?.Nodes.FirstOrDefault(n => n.Id == cmd.Target);
            if (graphNode != null)
            {
                graphNode.X = element.X;
                graphNode.Y = element.Y;
            }
        }
    }

    private void ApplyAddEdge(SemanticPresentation p, ActionCommand cmd)
    {
        var slideIndex = (cmd.Slide ?? 1) - 1;
        if (slideIndex < 0 || slideIndex >= p.Slides.Count) return;

        var slide = p.Slides[slideIndex];
        var from = GetStringParam(cmd, "from", "");
        var to = GetStringParam(cmd, "to", "");
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return;

        // Add to relationships
        if (!slide.Relationships.Any(r => r.From == from && r.To == to))
        {
            slide.Relationships.Add(new SemanticRelationship
            {
                From = from, To = to,
                Label = cmd.Value ?? "",
                Type = "added"
            });
        }

        // Add to graph
        if (slide.Graph != null && !slide.Graph.Edges.Any(e => e.From == from && e.To == to))
        {
            slide.Graph.Edges.Add(new SemanticGraphEdge
            {
                From = from, To = to,
                Label = cmd.Value ?? "",
                EdgeType = "flow",
                Confidence = 1.0
            });
        }
    }

    private void ApplyRemoveEdge(SemanticPresentation p, ActionCommand cmd)
    {
        var from = GetStringParam(cmd, "from", "");
        var to = GetStringParam(cmd, "to", "");

        foreach (var slide in p.Slides)
        {
            slide.Relationships.RemoveAll(r => r.From == from && r.To == to);
            slide.Graph?.Edges.RemoveAll(e => e.From == from && e.To == to);
        }
    }

    private async Task ApplyRewriteText(SemanticPresentation p, ActionCommand cmd)
    {
        var element = FindElement(p, cmd.Slide, cmd.Target);
        if (element == null) return;

        var newText = cmd.Value ?? GetStringParam(cmd, "newText", "");
        if (!string.IsNullOrEmpty(newText))
        {
            newText = newText.Replace("\\n", "\n");
            // Collapse runs of 2+ spaces into a single space, preserve \n line breaks
            newText = MultiSpaceRegex.Replace(newText, " ").Trim();

            // Word count validation logging
            var originalWordCount = element.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var newWordCount = newText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (newWordCount > originalWordCount)
            {
                Console.WriteLine($"[ActionExecutor] WARNING: Word count exceeded for {element.Id} — original: {originalWordCount}, new: {newWordCount}");
            }

            element.Text = newText;
        }
        else
        {
            var rewritten = await _llmService.ApplyTextRewriteAsync(element.Text, cmd.Description);
            rewritten = rewritten.Replace("\\n", "\n");
            // Collapse runs of 2+ spaces into a single space, preserve \n line breaks
            rewritten = MultiSpaceRegex.Replace(rewritten, " ").Trim();

            // Word count validation logging
            var originalWordCount = element.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var newWordCount = rewritten.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (newWordCount > originalWordCount)
            {
                Console.WriteLine($"[ActionExecutor] WARNING: Word count exceeded for {element.Id} — original: {originalWordCount}, new: {newWordCount}");
            }

            element.Text = rewritten;
        }
    }

    private void ApplyChangeStyle(SemanticPresentation p, ActionCommand cmd)
    {
        var element = FindElement(p, cmd.Slide, cmd.Target);
        if (element == null) return;

        var property = GetStringParam(cmd, "property", "fillColor");
        var value = GetStringParam(cmd, "value", cmd.Value ?? "");

        switch (property)
        {
            case "fillColor": element.FillColor = value; break;
            case "fontColor": element.FontColor = value; break;
            case "fontSize": if (double.TryParse(value, out var fs)) element.FontSize = fs; break;
        }
    }

    private void ApplyFontSize(SemanticPresentation p, ActionCommand cmd)
    {
        var element = FindElement(p, cmd.Slide, cmd.Target);
        if (element == null) return;
        if (cmd.Parameters.TryGetValue("newSize", out var sizeObj))
        {
            if (sizeObj is JsonElement j && j.TryGetDouble(out var size)) element.FontSize = size;
            else if (double.TryParse(sizeObj?.ToString(), out var parsed)) element.FontSize = parsed;
        }
    }

    private void ApplyChangeColor(SemanticPresentation p, ActionCommand cmd)
    {
        var element = FindElement(p, cmd.Slide, cmd.Target);
        if (element == null) return;
        var property = GetStringParam(cmd, "property", "fontColor");
        var color = GetStringParam(cmd, "newColor", "#000000");
        if (property == "fillColor") element.FillColor = color;
        else element.FontColor = color;
    }

    private void ApplyAddSlide(SemanticPresentation p, ActionCommand cmd)
    {
        var newSlide = new SemanticSlide
        {
            Id = p.Slides.Count + 1,
            Title = cmd.Value ?? GetStringParam(cmd, "title", "New Slide"),
            Classification = GetStringParam(cmd, "classification", "content")
        };
        p.Slides.Add(newSlide);
        p.SlideCount = p.Slides.Count;
    }

    private void ApplyRemoveSlide(SemanticPresentation p, ActionCommand cmd)
    {
        if (cmd.Slide == null) return;
        var idx = cmd.Slide.Value - 1;
        if (idx >= 0 && idx < p.Slides.Count)
        {
            p.Slides.RemoveAt(idx);
            p.SlideCount = p.Slides.Count;
            // Re-number
            for (int i = 0; i < p.Slides.Count; i++)
                p.Slides[i].Id = i + 1;
        }
    }

    private void ApplyReorderSlides(SemanticPresentation p, ActionCommand cmd)
    {
        if (!cmd.Parameters.TryGetValue("newOrder", out var orderObj)) return;
        List<int>? newOrder = null;
        if (orderObj is JsonElement je && je.ValueKind == JsonValueKind.Array)
            newOrder = je.EnumerateArray().Where(e => e.TryGetInt32(out _)).Select(e => e.GetInt32()).ToList();

        if (newOrder == null || newOrder.Count != p.Slides.Count) return;
        var reordered = newOrder.Select(i => p.Slides[i - 1]).ToList();
        p.Slides = reordered;
        for (int i = 0; i < p.Slides.Count; i++) p.Slides[i].Id = i + 1;
    }

    private SemanticElement? FindElement(SemanticPresentation p, int? slideNum, string? id)
    {
        if (slideNum != null)
        {
            var idx = slideNum.Value - 1;
            if (idx >= 0 && idx < p.Slides.Count)
                return p.Slides[idx].Elements.FirstOrDefault(e => e.Id == id);
        }
        return p.Slides.SelectMany(s => s.Elements).FirstOrDefault(e => e.Id == id);
    }

    private ActionCommand CloneCommand(ActionCommand cmd) =>
        JsonSerializer.Deserialize<ActionCommand>(JsonSerializer.Serialize(cmd))!;

    private string GetStringParam(ActionCommand cmd, string key, string def)
    {
        if (cmd.Parameters.TryGetValue(key, out var v))
            return v is JsonElement j ? j.GetString() ?? def : v?.ToString() ?? def;
        return def;
    }

    private double GetDoubleParam(ActionCommand cmd, string key, double def)
    {
        if (cmd.Parameters.TryGetValue(key, out var v))
        {
            if (v is JsonElement j && j.TryGetDouble(out var d)) return d;
            if (double.TryParse(v?.ToString(), out var p)) return p;
        }
        return def;
    }
}
