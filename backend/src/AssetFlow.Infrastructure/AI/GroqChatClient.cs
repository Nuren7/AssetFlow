using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace AssetFlow.Infrastructure.AI;

public sealed class GroqChatClient(string? apiKey, string model) : IGroqChatClient
{
    private static readonly Uri GroqEndpoint = new("https://api.groq.com/openai/v1");

    public async Task<GroqChatResponse> CompleteAsync(
        IReadOnlyList<GroqChatMessage> messages,
        IReadOnlyList<GroqToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Set the GROQ_API_KEY environment variable to enable the AI assistant.");
        }

        var client = new ChatClient(
            model,
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = GroqEndpoint });
        var chatMessages = messages.Select(ToSdkMessage).ToArray();
        var options = new ChatCompletionOptions
        {
            AllowParallelToolCalls = false
        };

        foreach (var tool in tools)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(
                tool.Name,
                tool.Description,
                BinaryData.FromString(tool.ParametersJson)));
        }

        var result = await client.CompleteChatAsync(chatMessages, options, cancellationToken);
        var completion = result.Value;
        var toolCalls = completion.ToolCalls
            .Select(call => new GroqToolCall(
                call.Id,
                call.FunctionName,
                call.FunctionArguments.ToString()))
            .ToArray();
        var content = completion.Content.Count == 0 ? null : completion.Content[0].Text;

        return new GroqChatResponse(content, completion.FinishReason.ToString(), toolCalls);
    }

    private static ChatMessage ToSdkMessage(GroqChatMessage message)
    {
        return message.Role switch
        {
            "system" => new SystemChatMessage(message.Content),
            "user" => new UserChatMessage(message.Content),
            "tool" when message.ToolCallId is not null => new ToolChatMessage(message.ToolCallId, message.Content),
            "assistant" when message.ToolCalls is { Count: > 0 } => new AssistantChatMessage(
                message.ToolCalls.Select(call => ChatToolCall.CreateFunctionToolCall(
                    call.Id,
                    call.Name,
                    BinaryData.FromString(call.Arguments)))),
            "assistant" => new AssistantChatMessage(message.Content),
            _ => throw new InvalidOperationException($"Unsupported chat message role '{message.Role}'.")
        };
    }
}
