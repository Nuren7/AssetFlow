namespace AssetFlow.Infrastructure.AI;

public interface IGroqChatClient
{
    Task<GroqChatResponse> CompleteAsync(
        IReadOnlyList<GroqChatMessage> messages,
        IReadOnlyList<GroqToolDefinition> tools,
        CancellationToken cancellationToken = default);
}

public sealed record GroqChatMessage(
    string Role,
    string Content,
    string? ToolCallId = null,
    IReadOnlyList<GroqToolCall>? ToolCalls = null);

public sealed record GroqToolCall(string Id, string Name, string Arguments);

public sealed record GroqToolDefinition(string Name, string Description, string ParametersJson);

public sealed record GroqChatResponse(string? Content, string FinishReason, IReadOnlyList<GroqToolCall> ToolCalls);
