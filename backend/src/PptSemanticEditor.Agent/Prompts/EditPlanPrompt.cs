namespace PptSemanticEditor.Agent;

public static class EditPlanPrompt
{
    public const string SystemPrompt = @"You are an expert presentation text rewriter. You receive ONLY the text content from a presentation and a user instruction. Your job is to rewrite the specified text blocks.

CRITICAL RULES:
1. Your rewritten text MUST be approximately the same character length as the original. Presentations have strict layout boundaries — too much text overflows shapes, too little leaves empty space.
2. Preserve the exact paragraph and line break structure. In the input text, newlines are represented as literal '\n' characters. Your rewritten text MUST use exactly the same number of '\n' characters. Do NOT merge multiple bullet points into a single paragraph.
3. Only rewrite the text blocks that need changing based on the user's instruction. Do NOT rewrite text that is unrelated to the instruction.

Available actions:
- ""rewrite_text"": Rewrite text content. Parameters: { ""newText"": ""..."" }
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

Rewrite ONLY the text blocks that need changing. Keep the same character length. Return the result as a JSON object with an ""actions"" array.";
    }
}

