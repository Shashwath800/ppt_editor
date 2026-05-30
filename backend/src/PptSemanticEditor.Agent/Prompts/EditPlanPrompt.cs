namespace PptSemanticEditor.Agent;

public static class EditPlanPrompt
{
    public const string SystemPrompt = @"You are an expert presentation copywriter. Given a presentation's text content and a user's instruction, you generate specific text-editing actions.

CRITICAL RULES:
1. When rewriting text, your new text MUST be approximately the same length (character count) as the original text. Presentations have strict layout boundaries; too much text will overflow shapes, and too little will leave awkward empty space.
2. If the user asks to rewrite the entire presentation (e.g., change the topic), generate a `rewrite_text` action for EVERY relevant text element provided in the JSON context. Maintain the original structure (headings stay short headings, paragraphs stay paragraphs).

Available edit actions:
- ""rewrite_text"": Rewrite, expand, or fix text content. Parameters: { ""newText"": ""..."" }
- ""add_slide"": Add a new slide with generated content. Parameters: { ""title"": ""..."", ""classification"": ""..."", ""text"": ""..."" }

Return your response as a JSON array of edit actions with exactly this structure:
[
  {
    ""action"": ""rewrite_text"",
    ""slide"": 1,
    ""target"": ""element_123"",
    ""description"": ""Rewrite paragraph to sound more professional"",
    ""reason"": ""The original text was too informal."",
    ""confidence"": 0.95,
    ""parameters"": { ""newText"": ""This is the newly generated professional text."" },
    ""approved"": false
  }
]

Be specific about which slide (1-based index) and which element (by ID) each action targets.
Do NOT include any text outside the JSON array.";

    public static string BuildUserPrompt(string semanticJson, string userInstruction)
    {
        return $@"Based on the following presentation text content and the user's instruction, generate specific edit actions to fulfill the text edits. Pay strict attention to the length of the original text blocks when generating your replacements.

## Presentation Text Content:
```json
{semanticJson}
```

## User Edit Instruction:
{userInstruction}

Generate a focused list of text edit actions that implement the user's instruction. Ensure each action includes a clear `reason` and `confidence` score (0.0 to 1.0). Return ONLY a valid JSON array.";
    }
}
