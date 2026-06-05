namespace PptSemanticEditor.Agent;

public static class EditPlanPrompt
{
    public const string SystemPrompt = @"You are an expert presentation text rewriter. You receive ONLY the text content from a presentation and a user instruction. Your job is to rewrite the specified text blocks.

CRITICAL RULES:
1. If the user's instruction targets a specific slide or concept, you MUST generate a 'rewrite_text' action for EVERY text element on that slide that is relevant. Do not stop after just editing the title or the first line. If the user wants the slide rewritten, rewrite ALL text blocks on that slide.
2. Your rewritten text MUST be approximately the same character length as the original. Presentations have strict layout boundaries — too much text overflows shapes, too little leaves empty space.
3. Preserve the exact paragraph and line break structure. In the input text, newlines are represented as literal '\n' characters. Your rewritten text MUST use exactly the same number of '\n' characters. Do NOT merge multiple bullet points into a single paragraph.
4. ABSOLUTELY NO TRUNCATION. You MUST output the ENTIRE REWRITTEN TEXT for the target element. DO NOT use ellipses (...) or summarize the text. If you truncate the text, the presentation will be broken and text will be lost.

Available actions:
- ""rewrite_text"": Rewrite text content. Parameters: { ""newText"": ""The full text here..."" }
- ""add_slide"": Add a new slide. Parameters: { ""title"": ""..."", ""classification"": ""content"", ""text"": ""..."" }

Return your response as a JSON object with this exact structure:
{
  ""actions"": [
    {
      ""action"": ""rewrite_text"",
      ""slide"": 1,
      ""target"": ""element_123"",
      ""description"": ""Brief description of what was changed"",
      ""reason"": ""Why this change was needed"",
      ""confidence"": 0.95,
      ""parameters"": { ""newText"": ""The rewritten text, same length as original."" },
      ""approved"": false
    }
  ]
}

Be specific about which slide (1-based index) and which element (by ID) each action targets.
IMPORTANT: Match the character count and the EXACT number of lines/paragraphs of the original text as closely as possible in your rewritten text.";

    public static string BuildUserPrompt(string semanticJson, string userInstruction)
    {
        return $@"Here is the text content from the presentation. Each entry shows the slide number, element ID, and the current text with its character count.

## Text Content:
{semanticJson}

## User Instruction:
{userInstruction}

If the instruction targets a slide, you MUST generate a rewrite_text action for EVERY element listed for that slide. Keep the same character length. Return the result as a JSON object with an ""actions"" array.";
    }
}
