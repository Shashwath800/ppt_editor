using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PptSemanticEditor.Core.Interfaces;
using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Agent;

public class GroqSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama-3.3-70b-versatile";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}

public class GroqLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly GroqSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GroqLlmService(HttpClient httpClient, IOptions<GroqSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
    }

    public async Task<AnalysisResult> AnalyzeAsync(string semanticJson)
    {
        var responseText = await ChatCompletionAsync(
            AnalysisPrompt.SystemPrompt,
            AnalysisPrompt.BuildUserPrompt(semanticJson)
        );

        try
        {
            var cleaned = ExtractJson(responseText);
            var result = JsonSerializer.Deserialize<AnalysisResult>(cleaned, JsonOptions);
            return result ?? new AnalysisResult
            {
                Analysis = new List<string> { "Unable to parse analysis response" },
                Summary = "Analysis completed but response format was unexpected"
            };
        }
        catch (JsonException)
        {
            return new AnalysisResult
            {
                Analysis = new List<string> { responseText },
                Summary = "Raw analysis response (could not parse structured format)"
            };
        }
    }

    public async Task<List<EditAction>> GenerateEditPlanAsync(string semanticJson, string userPrompt)
    {
        var responseText = await ChatCompletionAsync(
            EditPlanPrompt.SystemPrompt,
            EditPlanPrompt.BuildUserPrompt(semanticJson, userPrompt)
        );

        try
        {
            var cleaned = ExtractJson(responseText);
            
            // The LLM may return either a bare array [...] or an object { "actions": [...] }
            // because response_format: json_object forces a top-level object
            List<EditAction>? actions = null;
            
            var trimmed = cleaned.TrimStart();
            if (trimmed.StartsWith("["))
            {
                actions = JsonSerializer.Deserialize<List<EditAction>>(cleaned, JsonOptions);
            }
            else if (trimmed.StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(cleaned);
                // Look for an "actions" key containing the array
                if (doc.RootElement.TryGetProperty("actions", out var actionsElement))
                {
                    actions = JsonSerializer.Deserialize<List<EditAction>>(actionsElement.GetRawText(), JsonOptions);
                }
                else
                {
                    // Try other common wrapper keys
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            actions = JsonSerializer.Deserialize<List<EditAction>>(prop.Value.GetRawText(), JsonOptions);
                            break;
                        }
                    }
                }
            }
            
            return actions ?? new List<EditAction>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[EditPlan] JSON parse error: {ex.Message}");
            Console.WriteLine($"[EditPlan] Raw response: {responseText}");
            return new List<EditAction>
            {
                new EditAction
                {
                    Action = "error",
                    Description = $"Could not parse edit plan response: {responseText}",
                    Approved = false
                }
            };
        }
    }

    public async Task<string> ApplyTextRewriteAsync(string originalText, string instruction)
    {
        var systemPrompt = "You are a text rewriting assistant. Rewrite the given text according to the instruction. Return ONLY the rewritten text, nothing else.";
        var userPrompt = $"Original text: \"{originalText}\"\n\nInstruction: {instruction}\n\nRewritten text:";

        return await ChatCompletionAsync(systemPrompt, userPrompt);
    }

    private async Task<string> ChatCompletionAsync(string systemPrompt, string userPrompt)
    {
        var requestBody = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.3,
            max_tokens = 8000,
            response_format = new { type = "json_object" }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/openai/v1/chat/completions", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Groq API error ({response.StatusCode}): {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        var messageContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return messageContent ?? string.Empty;
    }

    /// <summary>
    /// Extracts JSON from a response that might contain markdown code fences.
    /// </summary>
    private string ExtractJson(string text)
    {
        text = text.Trim();

        // Remove markdown code fences if present
        if (text.StartsWith("```json"))
            text = text[7..];
        else if (text.StartsWith("```"))
            text = text[3..];

        if (text.EndsWith("```"))
            text = text[..^3];

        return text.Trim();
    }
}
