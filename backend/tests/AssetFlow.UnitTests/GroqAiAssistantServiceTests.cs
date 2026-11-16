using AssetFlow.Application.DTOs;
using AssetFlow.Application.Tools;
using AssetFlow.Infrastructure.AI;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AssetFlow.UnitTests;

public sealed class GroqAiAssistantServiceTests
{
    [Fact]
    public async Task ChatAsync_ExecutesRequestedToolAndReturnsFinalAnswer()
    {
        var client = new QueueGroqChatClient(
            new GroqChatResponse(
                null,
                "tool_calls",
                [new GroqToolCall("call-1", "get_assets_summary", "{}")]),
            new GroqChatResponse("There are 18 assets.", "stop", []));
        var executor = new Mock<IAssetToolExecutor>();
        executor.Setup(tool => tool.ExecuteAsync(
                "get_assets_summary",
                It.IsAny<System.Text.Json.JsonElement>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"totalAssets":18}""");
        var service = new GroqAiAssistantService(client, executor.Object, NullLogger<GroqAiAssistantService>.Instance);

        var response = await service.ChatAsync(new ChatRequest("How many assets do we have?"));

        response.Answer.Should().Be("There are 18 assets.");
        response.ToolsUsed.Should().ContainSingle()
            .Which.Should().Be(new ToolExecutionLog("get_assets_summary", "{}", "Asset data retrieved."));
        client.Requests.Should().HaveCount(2);
        client.Requests[1].Messages.Should().Contain(message => message.Role == "tool" && message.ToolCallId == "call-1");
    }

    [Fact]
    public async Task ChatAsync_WhenGroqFails_ReturnsFallbackWithoutThrowing()
    {
        var client = new QueueGroqChatClient(new HttpRequestException("Service unavailable"));
        var service = new GroqAiAssistantService(
            client,
            Mock.Of<IAssetToolExecutor>(),
            NullLogger<GroqAiAssistantService>.Instance);

        var response = await service.ChatAsync(new ChatRequest("Show available laptops."));

        response.Answer.Should().Contain("temporarily unavailable");
        response.ToolsUsed.Should().BeEmpty();
    }

    [Fact]
    public async Task ChatAsync_WhenToolArgumentsAreInvalid_ReportsToolErrorAndContinues()
    {
        var client = new QueueGroqChatClient(
            new GroqChatResponse(null, "tool_calls", [new GroqToolCall("call-1", "search_assets", "{")]),
            new GroqChatResponse("I could not complete the search.", "stop", []));
        var service = new GroqAiAssistantService(
            client,
            Mock.Of<IAssetToolExecutor>(),
            NullLogger<GroqAiAssistantService>.Instance);

        var response = await service.ChatAsync(new ChatRequest("Find an asset."));

        response.Answer.Should().Be("I could not complete the search.");
        response.ToolsUsed.Should().ContainSingle()
            .Which.ResultSummary.Should().Be("Tool arguments must be valid JSON.");
    }

    private sealed class QueueGroqChatClient(params object[] responses) : IGroqChatClient
    {
        private readonly Queue<object> _responses = new(responses);
        public List<(IReadOnlyList<GroqChatMessage> Messages, IReadOnlyList<GroqToolDefinition> Tools)> Requests { get; } = [];

        public Task<GroqChatResponse> CompleteAsync(
            IReadOnlyList<GroqChatMessage> messages,
            IReadOnlyList<GroqToolDefinition> tools,
            CancellationToken cancellationToken = default)
        {
            Requests.Add((messages, tools));
            var response = _responses.Dequeue();
            return response is Exception exception
                ? Task.FromException<GroqChatResponse>(exception)
                : Task.FromResult((GroqChatResponse)response);
        }
    }
}
