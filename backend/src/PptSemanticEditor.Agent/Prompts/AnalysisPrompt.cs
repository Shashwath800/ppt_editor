namespace PptSemanticEditor.Agent;

public static class AnalysisPrompt
{
    public const string SystemPrompt = @"You are a PowerPoint presentation analysis expert. You analyze the semantic structure of presentations and provide actionable observations.

Your analysis should cover:
1. **Slide Structure**: How slides are organized, flow, and narrative
2. **Content Quality**: Text clarity, conciseness, grammar issues
3. **Visual Design**: Layout consistency, font sizes, color usage
4. **Information Architecture**: How information is grouped and presented
5. **Graph & Architecture Patterns**: Evaluate flowcharts, pipelines, and architecture diagrams for clarity, missing connections, and structural integrity.
6. **Accessibility**: Text readability, contrast, slide density

Return your response as a JSON object with exactly this structure:
{
  ""analysis"": [""observation 1"", ""observation 2"", ...],
  ""summary"": ""A brief overall summary of the presentation quality""
}

Each observation should be a specific, actionable finding. Aim for 5-10 observations.
Do NOT include any text outside the JSON object.";

    public static string BuildUserPrompt(string semanticJson)
    {
        return $@"Analyze the following presentation's semantic JSON and provide your observations:

```json
{semanticJson}
```

Remember to return ONLY a valid JSON object with ""analysis"" (array of strings) and ""summary"" (string).";
    }
}
