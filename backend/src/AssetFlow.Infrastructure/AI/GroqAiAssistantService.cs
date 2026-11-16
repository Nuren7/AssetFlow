using System.Text.Json;
using AssetFlow.Application.DTOs;
using AssetFlow.Application.Exceptions;
using AssetFlow.Application.Services;
using AssetFlow.Application.Tools;
using Microsoft.Extensions.Logging;

namespace AssetFlow.Infrastructure.AI;

public sealed class GroqAiAssistantService(
    IGroqChatClient chatClient,
    IAssetToolExecutor toolExecutor,
    ILogger<GroqAiAssistantService> logger) : IAiAssistantService
{
    private const int MaximumToolRounds = 5;
    private const string FallbackAnswer = "The assistant is temporarily unavailable. Please try again later.";
    private const string ToolLimitAnswer = "I could not complete that request within the available tool steps. Please try a more specific question.";

    private static readonly IReadOnlyList<GroqToolDefinition> ToolDefinitions =
    [
        new(
            "get_assets_summary",
            "Get overall asset counts by status and category, including warranties expiring within 30 days.",
            """{"type":"object","properties":{},"additionalProperties":false}"""),
        new(
            "search_assets",
            "Search assets by name, model, or serial number and optionally filter by category, status, and manufacturer.",
            """{"type":"object","properties":{"search":{"type":"string"},"category":{"type":"string","enum":["Laptop","Monitor","Mobile","Accessory","Network","Other"]},"status":{"type":"string","enum":["Available","Assigned","InRepair","Retired"]},"manufacturer":{"type":"string"}},"additionalProperties":false}"""),
        new(
            "get_expiring_warranties",
            "List assets with warranties expiring within the next number of days. Defaults to 30 days.",
            """{"type":"object","properties":{"daysThreshold":{"type":"integer","minimum":1,"maximum":365}},"additionalProperties":false}"""),
        new(
            "get_asset_by_serial_number",
            "Find one asset by its exact serial number.",
            """{"type":"object","properties":{"serialNumber":{"type":"string"}},"required":["serialNumber"],"additionalProperties":false}""")
    ];

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new AssetValidationException(new Dictionary<string, string[]>
            {
                [nameof(request.Message)] = ["Message is required."]
            });
        }

        if (request.Message.Length > 4000)
        {
            throw new AssetValidationException(new Dictionary<string, string[]>
            {
                [nameof(request.Message)] = ["Message cannot exceed 4000 characters."]
            });
        }

        var messages = new List<GroqChatMessage>
        {
            new("system", "You are AssetFlow's IT asset assistant. Use only the provided read-only tools for asset facts. Never invent asset data. Explain results clearly and concisely. If a tool reports an error, communicate that limitation honestly."),
            new("user", request.Message.Trim())
        };
        var toolsUsed = new List<ToolExecutionLog>();

        try
        {
            for (var round = 0; round < MaximumToolRounds; round++)
            {
                var response = await chatClient.CompleteAsync(messages, ToolDefinitions, cancellationToken);
                if (response.ToolCalls.Count == 0)
                {
                    var answer = string.IsNullOrWhiteSpace(response.Content) ? FallbackAnswer : response.Content.Trim();
                    return new ChatResponse(answer, toolsUsed);
                }

                messages.Add(new GroqChatMessage("assistant", response.Content ?? string.Empty, ToolCalls: response.ToolCalls));
                foreach (var call in response.ToolCalls)
                {
                    var result = await ExecuteToolAsync(call, cancellationToken);
                    toolsUsed.Add(new ToolExecutionLog(call.Name, call.Arguments, CreateResultSummary(result)));
                    messages.Add(new GroqChatMessage("tool", result, call.Id));
                }
            }

            return new ChatResponse(ToolLimitAnswer, toolsUsed);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "The Groq assistant request failed.");
            return new ChatResponse(FallbackAnswer, toolsUsed);
        }
    }

    private async Task<string> ExecuteToolAsync(GroqToolCall call, CancellationToken cancellationToken)
    {
        try
        {
            using var arguments = JsonDocument.Parse(call.Arguments);
            return await toolExecutor.ExecuteAsync(call.Name, arguments.RootElement, cancellationToken);
        }
        catch (ToolArgumentException exception)
        {
            logger.LogInformation(exception, "The assistant returned invalid arguments for tool {ToolName}.", call.Name);
            return JsonSerializer.Serialize(new { error = exception.Message });
        }
        catch (JsonException exception)
        {
            logger.LogInformation(exception, "The assistant returned malformed JSON for tool {ToolName}.", call.Name);
            return JsonSerializer.Serialize(new { error = "Tool arguments must be valid JSON." });
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Asset assistant tool {ToolName} failed.", call.Name);
            return JsonSerializer.Serialize(new { error = "The asset lookup could not be completed." });
        }
    }

    private static string CreateResultSummary(string result)
    {
        try
        {
            using var document = JsonDocument.Parse(result);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                return root.GetArrayLength() is var count && count == 0
                    ? "No matching assets found."
                    : $"Returned {root.GetArrayLength()} asset records.";
            }

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var error))
            {
                return error.GetString() ?? "Tool execution failed.";
            }

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("message", out _))
            {
                return "No asset matched the exact serial number.";
            }

            return "Asset data retrieved.";
        }
        catch (JsonException)
        {
            return "Tool completed with an unstructured response.";
        }
    }
}
